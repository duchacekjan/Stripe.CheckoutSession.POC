using FastEndpoints;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using POC.Api.Common;
using POC.Api.Features.Inventory.Seed;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Orders.SetBookingProtection;

public static class SetBookingProtection
{
    public record Request(Guid BasketId, bool EnableProtection);

    public record Response(string Type, string? Message = null);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, EmptyResponse>
    {
        private readonly long _bookingProtectionPerformanceId = Seed.BookingProtection.Performances.First().Id;

        public override void Configure()
        {
            Post("/{basketId}/set-booking-protection");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<EmptyResponse>()
                .Produces<string>(StatusCodes.Status400BadRequest)
                .WithName($"{nameof(Orders)}.{nameof(SetBookingProtection)}")
            );
            Summary(s =>
            {
                s.Summary = "Sets booking protection for an order";
                s.Responses[StatusCodes.Status200OK] = "Booking protection updated successfully";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var session = await dbContext.CheckoutSessions
                .Where(o => o.Order.BasketId == req.BasketId)
                .Select(s => new { s.OrderId, s.SessionId })
                .FirstOrDefaultAsync(ct);

            if (session is null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            if (req.EnableProtection)
            {
                var hasPerformance = await dbContext.Seats
                    .Where(s => s.OrderItemId != null && s.OrderItem!.OrderId == session.OrderId && s.PerformanceId > 0)
                    .AnyAsync(ct);
                if (!hasPerformance)
                {
                    ValidationFailures.Add(new ValidationFailure(nameof(req.BasketId), "Booking protection can only be set for orders with performances."));
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                await AddBookingProtection(session.OrderId, ct);
            }
            else
            {
                var hasBookingProtection = await dbContext.Seats
                    .Where(s => s.OrderItemId != null && s.OrderItem!.OrderId == session.OrderId && s.PerformanceId == Seed.BookingProtection.Performances.First().Id)
                    .AnyAsync(ct);
                if (hasBookingProtection)
                {
                    await RemoveBookingProtection(session.OrderId, ct);
                }
            }

            await SendOkAsync(new EmptyResponse(), ct);
        }

        private async Task AddBookingProtection(long orderId, CancellationToken ct)
        {
            var orderItem = new OrderItem
            {
                OrderId = orderId,
                Seats =
                [
                    new Seat
                    {
                        Row = "BookingProtection",
                        Number = 0,
                        PriceId = Seed.BookingProtectionPriceId,
                        PerformanceId = _bookingProtectionPerformanceId
                    }
                ]
            };
            dbContext.OrderItems.Add(orderItem);
            await dbContext.SaveChangesAsync(ct);
        }

        private async Task RemoveBookingProtection(long orderId, CancellationToken ct)
        {
            await dbContext.Seats
                .Where(s => s.OrderItemId != null && s.OrderItem!.OrderId == orderId && s.PerformanceId == _bookingProtectionPerformanceId)
                .ExecuteDeleteAsync(ct);
        }
    }
}