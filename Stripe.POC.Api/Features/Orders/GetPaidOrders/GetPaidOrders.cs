using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Orders.GetPaidOrders;

public static class GetPaidOrders
{
    public record Response(List<PaidOrdersDTO> PaidOrders);

    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/paid-orders");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(GetPaidOrders)}")
            );
            Summary(s =>
            {
                s.Summary = "Retrieves all paid orders";
                s.Responses[StatusCodes.Status200OK] = "Paid orders retrieved successfully";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var orders = await dbContext.Seats
                .Where(w => w.OrderItemId != null)
                .Where(w => w.OrderItem!.Order.Status == OrderStatus.Paid)
                .Select(seat =>
                    new
                    {
                        seat.Performance.EventId, EventName = seat.Performance.Event.Name, seat.PerformanceId, seat.Performance.PerformanceDate, seat.PriceId, Price = seat.Price.Amount,
                        SeatId = seat.Id, SeatRow = seat.Row,
                        SeatNumber = seat.Number,
                        OrderId = seat.OrderItem!.Order.Id,
                        seat.OrderItem!.Order.BasketId
                    })
                .ToListAsync(ct);

            var paidOrders = orders
                .GroupBy(g => new { g.BasketId, g.OrderId })
                .Select(s => new PaidOrdersDTO(
                    s.Key.OrderId,
                    s.Key.BasketId,
                    s.Select(t => new TicketDTO(t.EventId, t.EventName, t.PerformanceId, t.PerformanceDate, t.PriceId, t.Price, t.SeatId, t.SeatRow, t.SeatNumber)).ToList(),
                    s.Sum(t => t.Price)
                ))
                .ToList();

            await SendOkAsync(new Response(paidOrders), ct);
        }
    }
}