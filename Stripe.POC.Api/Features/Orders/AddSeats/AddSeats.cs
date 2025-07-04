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
    }
}