using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Inventory.GetSeats;

public static class GetSeats
{
    public record Response(List<SeatListDTO> Seats);

    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/performances/{performanceId}/seats");
            Group<InventoryGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Inventory)}.{nameof(GetSeats)}")
            );
            Summary(s =>
            {
                s.Summary = "Returns list of seats for given performance";
                s.Responses[StatusCodes.Status200OK] = "List of seats";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var performanceId = Route<long>("performanceId");
            var seats = await dbContext.Seats
                .AsNoTracking()
                .Include(i => i.Price)
                .Where(s => s.PerformanceId == performanceId)
                .OrderBy(o => o.PriceId)
                .ThenBy(o => o.Row)
                .ThenBy(o => o.Number)
                .Select(s => new SeatListDTO(s.Id, s.Row, s.Number, s.PriceId, s.Price.Amount, s.OrderItemId == null))
                .ToListAsync(ct);
            await SendAsync(new Response(seats), cancellation: ct);
        }
    }
}