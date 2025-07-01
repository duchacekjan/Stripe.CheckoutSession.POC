using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Persistence;

namespace POC.Api.Features.Orders.Shared;

internal static class OrderExtensions
{
    public static async Task<List<TicketDTO>> OrderTicketsAsync(
        this AppDbContext dbContext, Guid basketId, CancellationToken cancellationToken = default) =>
        await dbContext.Seats
            .Where(w => w.OrderItemId != null)
            .Where(w => w.OrderItem!.Order.BasketId == basketId)
            .Select(seat =>
                new TicketDTO(seat.Performance.EventId, seat.Performance.Event.Name, seat.PerformanceId, seat.Performance.PerformanceDate, seat.PriceId, seat.Price.Amount, seat.Id, seat.Row,
                    seat.Number))
            .ToListAsync(cancellationToken);
}