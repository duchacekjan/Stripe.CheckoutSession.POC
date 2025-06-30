using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe.Checkout;

namespace POC.Api.Features.Orders.AddSeats;

public static class AddSeats
{
    public record Request(Guid BasketId, long[] SeatIds);

    public record Response(Guid BasketId, decimal TotalPrice);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/{BasketId}/add-seats");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(AddSeats)}")
            );
            Summary(s =>
            {
                s.Summary = "Adds seats to an existing order";
                s.Responses[StatusCodes.Status200OK] = "Seats added successfully";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var order = await dbContext.Orders
                .Where(o => o.BasketId == req.BasketId)
                .Select(s => (long?)s.Id)
                .FirstOrDefaultAsync(ct);

            if (!order.HasValue)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var items = await CreateOrderItems(req.SeatIds, order.Value);
            dbContext.OrderItems.AddRange(items);

            await dbContext.SaveChangesAsync(ct);

            await UpdateLineItems(order.Value, req.SeatIds, items, ct);

            var totalPrice = await dbContext.OrderItems
                .Where(o => o.OrderId == order.Value)
                .SumAsync(o => o.Seats.Sum(s => s.Price.Amount), cancellationToken: ct);
            await SendOkAsync(new Response(req.BasketId, totalPrice), ct);
        }

        private async Task<List<OrderItem>> CreateOrderItems(long[] seatIds, long orderId)
        {
            var seats = await dbContext.Seats
                .Where(s => seatIds.Contains(s.Id))
                .ToListAsync();

            var orderItems = new List<OrderItem>();
            foreach (var seatGroup in seats.GroupBy(g => g.PriceId))
            {
                var orderItem = new OrderItem
                {
                    OrderId = orderId
                };
                orderItems.Add(orderItem);

                foreach (var seat in seatGroup)
                {
                    seat.OrderItem = orderItem;
                }
            }

            return orderItems;
        }

        private async Task UpdateLineItems(long orderId, long[] seatIds, List<OrderItem> items, CancellationToken cancellationToken)
        {
            var sessionId = await dbContext.CheckoutSessions
                .Where(w => w.OrderId == orderId)
                .Select(s => s.SessionId)
                .FirstOrDefaultAsync(cancellationToken);
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            var info = await dbContext.Seats
                .Where(w => seatIds.Contains(w.Id))
                .Select(s => new
                {
                    s.Id, EventId = s.Performance.Event.Id, s.Performance.Event.Name, s.Performance.PerformanceDate, s.PerformanceId, s.PriceId, s.OrderItemId, s.Price.Amount, s.Row, s.Number
                })
                .ToListAsync(cancellationToken);

            var service = new SessionService();
            var lineItems = await service.LineItems.ListAsync(sessionId, cancellationToken: cancellationToken);
            var newLineItems = new List<SessionLineItemOptions>();
            foreach (var orderItem in items)
            {
                var seats = info.GroupBy(f => f.OrderItemId)
                    .First(f => f.Key == orderItem.Id);
                var orderItemInfo = seats.First();
                var item = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(orderItemInfo.Amount * 100),
                        Currency = "gbp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = orderItemInfo.Name,
                            Description = Description(orderItemInfo.PerformanceId, orderItemInfo.PerformanceDate,
                                seats.Select(s => (s.Row, s.Number))), //$"Performance date: {orderItemInfo.PerformanceDate}\nSeats: {string.Join(", ", seats.Select(s => $"{s.Row}{s.Number}"))}",
                            Metadata = new Dictionary<string, string>
                            {
                                { "eventId", orderItemInfo.EventId.ToString() },
                                { "performanceId", orderItemInfo.PerformanceId.ToString() },
                                { "priceId", orderItemInfo.PriceId.ToString() },
                            }
                        }
                    },
                    Quantity = orderItem.Seats.Count
                };
                newLineItems.Add(item);
            }

            var updatedItems = lineItems.Select(s => new SessionLineItemOptions
            {
                Id = s.Id
            }).ToList();
            updatedItems.AddRange(newLineItems);
            var updateOptions = new SessionUpdateOptions
            {
                LineItems = updatedItems
            };

            await service.UpdateAsync(sessionId, updateOptions, cancellationToken: cancellationToken);
        }

        private static string Description(long performanceId, DateTime performanceDate, IEnumerable<(string SeatRow, uint SeatNumber)> tickets) =>
            performanceId == -1
                ? $"Codes: {string.Join(", ", tickets.Select(s => $"{s.SeatRow}"))}"
                : $"Performance date: {performanceDate}\nSeats: {string.Join(", ", tickets.Select(s => $"{s.SeatRow}{s.SeatNumber}"))}";
    }
}