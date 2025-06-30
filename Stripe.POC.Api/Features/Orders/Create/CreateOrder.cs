using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Order = POC.Api.Persistence.Entities.Order;

namespace POC.Api.Features.Orders.Create;

public static class CreateOrder
{
    public record Request(long[] SeatIds);

    public record Response(Guid BasketId, decimal TotalPrice);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/create");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(CreateOrder)}")
            );
            Summary(s =>
            {
                s.Summary = "Creates a new order";
                s.Responses[StatusCodes.Status201Created] = "Order created successfully";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var order = new Order
            {
                BasketId = Guid.NewGuid(),
                Status = OrderStatus.Created,
                OrderItems = await CreateOrderItems(req.SeatIds)
            };
            var entity = dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(ct);
            var totalPrice = await dbContext.OrderItems
                .Where(o => o.OrderId == entity.Entity.Id)
                .SumAsync(o => o.Seats.Sum(s => s.Price.Amount), cancellationToken: ct);
            await SendAsync(new Response(order.BasketId, totalPrice), StatusCodes.Status201Created, ct);
        }

        private async Task<List<OrderItem>> CreateOrderItems(long[] seatIds)
        {
            var seats = await dbContext.Seats
                .Where(s => seatIds.Contains(s.Id))
                .ToListAsync();
            var orderItems = new List<OrderItem>();
            foreach (var seatGroup in seats.GroupBy(g => g.PriceId))
            {
                var orderItem = new OrderItem();
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