using FastEndpoints;
using POC.Api.DTOs;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;

namespace POC.Api.Features.Orders.GetTickets;

public static class GetOrder
{
    public record Response(Guid BasketId, List<TicketDTO> Tickets, decimal TotalPrice);

    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/{basketId}/tickets");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(GetOrder)}")
            );
            Summary(s =>
            {
                s.Summary = "Retrieves tickets from an order by its basket ID";
                s.Responses[StatusCodes.Status200OK] = "Tickets retrieved successfully";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var basketId = Route<Guid>("basketId");
            var tickets = await dbContext.OrderTicketsAsync(basketId, ct);

            if (tickets.Count == 0)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var response = new Response(basketId, tickets, tickets.Sum(s => s.Price));

            await SendOkAsync(response, ct);
        }
    }
}