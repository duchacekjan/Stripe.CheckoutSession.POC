using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Common;
using POC.Api.DTOs;
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
                    s.Id, EventId = s.Performance.Event.Id, EventName = s.Performance.Event.Name, s.Performance.PerformanceDate, s.PerformanceId, s.PriceId, s.OrderItemId, s.Price.Amount, s.Row,
                    s.Number
                })
                .ToListAsync(cancellationToken);

            var service = new SessionService();
            var lineItems = await service.LineItems.ListAsync(sessionId, cancellationToken: cancellationToken);

            var updatedItems = lineItems.Select(s => new SessionLineItemOptions
            {
                Id = s.Id,
                Quantity = s.Quantity
            }).ToList();

            var groupedTickets = info
                .GroupBy(k => k.OrderItemId.GetValueOrDefault())
                .ToDictionary(k => k.Key, v => v.Select(s => new TicketDTO(s.EventId, s.EventName, s.PerformanceId, s.PerformanceDate, s.PriceId, s.Amount, s.Id, s.Row, s.Number)).ToList());
            foreach (var ticketsGroup in groupedTickets)
            {
                foreach (var lineItem in lineItems)
                {
                    var tickets = ticketsGroup.Value;
                    var priceBandTickets = lineItem.MatchingTickets(tickets);
                    if (priceBandTickets.Count == 0)
                    {
                        var newItem = tickets.ToLineItem();
                        updatedItems.Add(newItem);
                    }
                    else
                    {
                        var existingItem = updatedItems.First(f => f.Id == lineItem.Id);
                        existingItem.Quantity = lineItem.Quantity + priceBandTickets.Count;
                    }
                }
            }

            var updateOptions = new SessionUpdateOptions
            {
                LineItems = updatedItems
            };

            await service.UpdateAsync(sessionId, updateOptions, cancellationToken: cancellationToken);
        }
    }
}