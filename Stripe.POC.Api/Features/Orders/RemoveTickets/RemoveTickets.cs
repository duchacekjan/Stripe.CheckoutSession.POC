using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Common;
using POC.Api.DTOs;
using POC.Api.Features.Inventory.Seed;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Features.Orders.RemoveTickets;

public static class RemoveTickets
{
    public record Request(Guid BasketId, List<long> SeatIds);

    public record Response(UpdateStatus Status);

    public enum UpdateStatus
    {
        Updated,
        Emptied,
        Error
    }

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/{basketId}/remove-tickets");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(RemoveTickets)}")
            );
            Summary(s =>
            {
                s.Summary = "Removes tickets from and order";
                s.Responses[StatusCodes.Status200OK] = "Tickets removed successfully";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            if (await dbContext.Orders.Where(w => w.BasketId == req.BasketId).AnyAsync(ct) == false)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            await RemoveTicketsFromOrder(req.BasketId, req.SeatIds, ct);
            var tickets = await dbContext.OrderTicketsAsync(req.BasketId, ct);
            var remainingTickets = tickets
                .Any(w => w.Value.Count > 0);
            var status = remainingTickets ? UpdateStatus.Updated : UpdateStatus.Emptied;
            var response = new Response(status);
            switch (response.Status)
            {
                case UpdateStatus.Emptied:
                    await dbContext.Orders
                        .Where(w => w.BasketId == req.BasketId)
                        .ExecuteUpdateAsync(u => u.SetProperty(p => p.Status, OrderStatus.Cancelled), cancellationToken: ct);
                    break;
                case UpdateStatus.Error:
                    await SendAsync(response, StatusCodes.Status400BadRequest, ct);
                    return;
                case UpdateStatus.Updated:
                    await SendOkAsync(response, ct);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task RemoveTicketsFromOrder(Guid basketId, List<long> seatIds, CancellationToken cancellationToken)
        {
            var seatsToUpdate = await dbContext.Seats
                .Where(w => w.OrderItemId != null)
                .Where(w => w.OrderItem!.Order.BasketId == basketId)
                .Where(w => seatIds.Contains(w.Id))
                .ToListAsync(cancellationToken);
            foreach (var seat in seatsToUpdate)
            {
                seat.OrderItemId = null;
            }

            await HandleBookingProtection(seatsToUpdate, basketId, cancellationToken);
            await HandleVouchersAsync(seatsToUpdate, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleBookingProtection(List<Seat> seatsToUpdate, Guid basketId, CancellationToken cancellationToken)
        {
            var hasPerformance = await dbContext.Seats
                .Where(w => !seatsToUpdate.Select(s => s.Id).Contains(w.Id))
                .Where(w => w.OrderItemId != null)
                .Where(w => w.OrderItem!.Order.BasketId == basketId)
                .Where(w => w.PerformanceId > 0)
                .AnyAsync(cancellationToken);

            var hasBookingProtection = await dbContext.Seats
                .Where(w => w.OrderItemId != null)
                .Where(w => w.OrderItem!.Order.BasketId == basketId)
                .Where(w => w.PerformanceId == Seed.BookingProtection.Performances.First().Id)
                .AnyAsync(cancellationToken);

            if (!hasPerformance && hasBookingProtection)
            {
                var bookingProtection = await dbContext.Seats
                    .Where(w => w.OrderItemId != null)
                    .Where(w => w.OrderItem!.Order.BasketId == basketId)
                    .Where(w => w.PerformanceId == Seed.BookingProtection.Performances.First().Id)
                    .ToListAsync(cancellationToken);
                foreach (var seat in bookingProtection)
                {
                    seat.OrderItemId = null;
                }
            }
        }

        private Task HandleVouchersAsync(List<Seat> seatsToUpdate, CancellationToken cancellationToken)
        {
            var vouchersToRemoveIds = seatsToUpdate.Where(w => w.PerformanceId == Seed.Voucher.Performances.First().Id).Select(s => s.Id).ToList();

            return dbContext.Vouchers.Where(w => vouchersToRemoveIds.Contains(w.SeatId))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}