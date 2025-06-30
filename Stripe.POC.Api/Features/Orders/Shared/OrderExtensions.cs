using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Persistence;

namespace POC.Api.Features.Orders.Shared;

internal static class OrderExtensions
{
    public static async Task<List<TicketDTO>> OrderTicketsAsync(
        this AppDbContext dbContext, Guid basketId, CancellationToken cancellationToken = default)
    {
        var orderItemIds = await dbContext.OrderItems
            .Where(w => w.Order.BasketId == basketId)
            .Select(s => (long?)s.Id)
            .ToListAsync(cancellationToken);
        return await dbContext.Seats
            .Where(w => orderItemIds.Contains(w.OrderItemId))
            .Select(seat =>
                new TicketDTO(seat.Performance.EventId, seat.Performance.Event.Name, seat.PerformanceId, seat.Performance.PerformanceDate, seat.PriceId, seat.Price.Amount, seat.Id, seat.Row,
                    seat.Number))
            .ToListAsync(cancellationToken);
    }
}