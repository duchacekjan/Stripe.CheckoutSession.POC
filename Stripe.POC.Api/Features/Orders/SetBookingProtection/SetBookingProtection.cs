using FastEndpoints;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using POC.Api.Common;
using POC.Api.DTOs;
using POC.Api.Features.Inventory.Seed;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Features.Orders.SetBookingProtection;

public static class SetBookingProtection
{
    public record Request(Guid BasketId, bool EnableProtection);

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
            var orderId = await dbContext.Orders
                .Where(o => o.BasketId == req.BasketId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (orderId == 0)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var bookingProtectionChanged = false;
            if (req.EnableProtection)
            {
                var hasPerformance = await dbContext.Seats
                    .Where(s => s.OrderItemId != null && s.OrderItem!.OrderId == orderId && s.PerformanceId > 0)
                    .AnyAsync(ct);
                if (!hasPerformance)
                {
                    ValidationFailures.Add(new ValidationFailure(nameof(req.BasketId), "Booking protection can only be set for orders with performances."));
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                await AddBookingProtection(orderId, ct);
                bookingProtectionChanged = true;
            }
            else
            {
                var hasBookingProtection = await dbContext.Seats
                    .Where(s => s.OrderItemId != null && s.OrderItem!.OrderId == orderId && s.PerformanceId == Seed.BookingProtection.Performances.First().Id)
                    .AnyAsync(ct);
                if (hasBookingProtection)
                {
                    await RemoveBookingProtection(orderId, ct);
                    bookingProtectionChanged = true;
                }
            }

            if (bookingProtectionChanged)
            {
                await UpdateLineItems(orderId, req.EnableProtection, ct);
            }

            await SendOkAsync(new EmptyResponse(), ct);
        }

        private async Task UpdateLineItems(long orderId, bool hasBookingProtection, CancellationToken ct)
        {
            var sessionId = await dbContext.CheckoutSessions.Where(w => w.OrderId == orderId)
                .Select(s => s.SessionId)
                .FirstOrDefaultAsync(ct);

            if (sessionId == null)
            {
                return;
            }

            var service = new SessionService();
            var lineItems = await service.LineItems.ListAsync(sessionId, cancellationToken: ct);
            var updatedItems = hasBookingProtection
                ? await AddProtection(lineItems, ct)
                : RemoveProtection(lineItems);

            await service.UpdateAsync(sessionId, new SessionUpdateOptions { LineItems = updatedItems }, cancellationToken: ct);
        }

        private async Task<List<SessionLineItemOptions>> AddProtection(StripeList<LineItem> lineItems, CancellationToken ct)
        {
            var updatedItems = lineItems.Select(s => new SessionLineItemOptions { Id = s.Id, Quantity = s.Quantity }).ToList();
            var existingProtection = lineItems.FindLineItem(_bookingProtectionPerformanceId, Seed.BookingProtectionPriceId);
            if (existingProtection != null)
            {
                var item = updatedItems.First(f => f.Id == existingProtection.Id);
                item.Quantity += 1;
            }
            else
            {
                var price = await dbContext.Prices
                    .Where(p => p.Id == Seed.BookingProtectionPriceId)
                    .Select(s => s.Amount)
                    .FirstOrDefaultAsync(ct);

                var ticket = new TicketDTO(Seed.BookingProtection.Id, Seed.BookingProtection.Name, _bookingProtectionPerformanceId,
                    Seed.BookingProtection.Performances.First().PerformanceDate, Seed.BookingProtectionPriceId, price, 0, Seed.BookingProtection.Name, 0);
                var item = new[] { ticket }.ToLineItem();
                updatedItems.Add(item);
            }

            return updatedItems;
        }

        private List<SessionLineItemOptions> RemoveProtection(StripeList<LineItem> lineItems)
        {
            var updatedItems = lineItems.Select(s => new SessionLineItemOptions { Id = s.Id, Quantity = s.Quantity }).ToList();
            var existingProtection = lineItems.FindLineItem(_bookingProtectionPerformanceId, Seed.BookingProtectionPriceId);
            if (existingProtection != null)
            {
                var item = updatedItems.First(f => f.Id == existingProtection.Id);
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                }
                else
                {
                    updatedItems.Remove(item);
                }
            }

            return updatedItems;
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