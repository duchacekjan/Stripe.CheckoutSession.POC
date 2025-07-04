using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;

namespace POC.Api.Features.Orders.GetTickets;

public static class GetOrder
{
    public record Response(Guid BasketId, Dictionary<long, List<TicketDTO>> Tickets, List<VoucherDTO> RedeemedVouchers, decimal TotalPrice);

    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/{basketId}/content");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(GetOrder)}")
            );
            Summary(s =>
            {
                s.Summary = "Retrieves basket content by its basket ID";
                s.Responses[StatusCodes.Status200OK] = "Content retrieved successfully";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var basketId = Route<Guid>("basketId");
            var orderItemsTickets = await dbContext.OrderTicketsAsync(basketId, ct);

            var tickets = orderItemsTickets
                .Where(w => w.Value.Count > 0)
                .ToDictionary();
            if (tickets.Count == 0)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var vouchers = await dbContext.Orders
                .Where(w => w.BasketId == basketId)
                .SelectMany(s => s.Vouchers.Select(v => new VoucherDTO(v.VoucherId, v.Voucher.Seat.Row, v.Amount)))
                .ToListAsync(ct);

            var itemsTotal = tickets.Sum(s => s.Value.Sum(v => v.Price));
            var vouchersTotal = vouchers.Sum(v => v.Amount);
            var totalPrice = itemsTotal - vouchersTotal;
            var response = new Response(basketId, tickets, vouchers, totalPrice);

            await SendOkAsync(response, ct);
        }
    }
}