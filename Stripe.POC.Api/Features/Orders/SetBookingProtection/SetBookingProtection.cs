using FastEndpoints;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
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
                    .Where(s => s.OrderItemId != null && s.OrderItem!.OrderId == orderId && s.PerformanceId == Seed.BookingProtectionPerformanceId)
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
                : await RemoveProtection(lineItems, ct);

            await service.UpdateAsync(sessionId, new SessionUpdateOptions { LineItems = updatedItems }, cancellationToken: ct);
        }

        private async Task<List<SessionLineItemOptions>> AddProtection(StripeList<LineItem> lineItems, CancellationToken ct)
        {
            var updatedItems = lineItems.Select(s => new SessionLineItemOptions { Id = s.Id, Quantity = s.Quantity }).ToList();
            //TODO when metadata are present
            // var existingProtection = lineItems.Data
            //     .FirstOrDefault(f => f.Metadata.TryGetValue("performanceId", out var performanceId) && performanceId == Seed.BookingProtectionPerformanceId.ToString());

            var price = await dbContext.Prices
                .Where(p => p.Id == Seed.BookingProtectionPriceId)
                .Select(s => s.Amount)
                .FirstOrDefaultAsync(ct);

            var unitPrice = (long)(price * 100);
            var existingProtection = lineItems.Data
                .FirstOrDefault(f => f.Price.UnitAmount == unitPrice);
            if (existingProtection != null)
            {
                var item = updatedItems.First(f => f.Id == existingProtection.Id);
                item.Quantity += 1;
            }
            else
            {
                updatedItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Booking Protection",
                            Description = "Protect your booking against cancellations and changes."
                        },
                        Currency = "gbp",
                        UnitAmount = (long)price * 100,
                    },
                    Quantity = 1,
                    Metadata = new Dictionary<string, string>
                    {
                        { "performanceId", Seed.BookingProtectionPerformanceId.ToString() }
                    }
                });
            }

            return updatedItems;
        }

        private async Task<List<SessionLineItemOptions>> RemoveProtection(StripeList<LineItem> lineItems, CancellationToken ct)
        {
            var updatedItems = lineItems.Select(s => new SessionLineItemOptions { Id = s.Id, Quantity = s.Quantity }).ToList();
            //TODO when metadata are present
            // var existingProtection = lineItems.Data
            //     .FirstOrDefault(f => f.Metadata.TryGetValue("performanceId", out var performanceId) && performanceId == Seed.BookingProtectionPerformanceId.ToString());

            var price = await dbContext.Prices
                .Where(p => p.Id == Seed.BookingProtectionPriceId)
                .Select(s => s.Amount)
                .FirstOrDefaultAsync(ct);

            var unitPrice = (long)(price * 100);
            var existingProtection = lineItems.Data
                .FirstOrDefault(f => f.Price.UnitAmount == unitPrice);
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
                        PerformanceId = Seed.BookingProtectionPerformanceId
                    }
                ]
            };
            dbContext.OrderItems.Add(orderItem);
            await dbContext.SaveChangesAsync(ct);
        }

        private async Task RemoveBookingProtection(long orderId, CancellationToken ct)
        {
            await dbContext.Seats
                .Where(s => s.OrderItemId != null && s.OrderItem!.OrderId == orderId && s.PerformanceId == Seed.BookingProtectionPerformanceId)
                .ExecuteDeleteAsync(ct);
        }
    }
}