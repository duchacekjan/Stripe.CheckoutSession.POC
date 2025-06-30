using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Inventory.GetEvents;

public static class GetEvents
{
    public record Response(List<EventListDTO> Events);

    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/events");
            Group<InventoryGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Inventory)}.{nameof(GetEvents)}")
            );
            Summary(s =>
            {
                s.Summary = "Returns list of events";
                s.Responses[StatusCodes.Status200OK] = "List of events";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var events = await dbContext.Events
                .AsNoTracking()
                .Where(w => w.Id > 0)
                .Include(e => e.Performances)
                .Select(e =>
                    new EventListDTO(e.Id, e.Name, e.Performances
                        .Select(p => new PerformanceListDTO(p.Id, p.PerformanceDate, p.DurationMinutes))
                        .ToList())
                )
                .ToListAsync(ct);
            await SendOkAsync(new Response(events), ct);
        }
    }
}