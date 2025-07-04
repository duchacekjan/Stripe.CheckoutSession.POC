using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Persistence;

namespace POC.Api.Features.Orders.Shared;

internal static class OrderExtensions
{
    public static async Task<Dictionary<long, List<TicketDTO>>> OrderTicketsAsync(
        this AppDbContext dbContext, Guid basketId, CancellationToken cancellationToken = default) =>
        await dbContext.OrderItems
            .Where(w => w.Order.BasketId == basketId)
            .Include(i => i.Seats).ThenInclude(t => t.Performance).ThenInclude(t => t.Event)
            .Include(i => i.Seats).ThenInclude(t => t.Price)
            .ToDictionaryAsync(k => k.Id, v => v.Seats.Select(seat =>
                new TicketDTO(seat.Performance.EventId, seat.Performance.Event.Name, seat.PerformanceId, seat.Performance.PerformanceDate, seat.PriceId, seat.Price.Amount, seat.Id, seat.Row,
                    seat.Number)).ToList(), cancellationToken);
}