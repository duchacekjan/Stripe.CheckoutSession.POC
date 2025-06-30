using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Orders.SetPaid;

public static class SetPaid
{
    public record Request(Guid BasketId);

    public record Response(List<string> VoucherCodes);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/{basketId}/set-paid");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(SetPaid)}")
            );
            Summary(s =>
            {
                s.Summary = "Marks an order as paid and returns voucher codes";
                s.Responses[StatusCodes.Status200OK] = "Order marked as paid successfully";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var order = await dbContext.Orders
                .Where(o => o.BasketId == req.BasketId)
                .FirstOrDefaultAsync(ct);

            if (order == null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            order.Status = OrderStatus.Paid;
            await dbContext.SaveChangesAsync(ct);

            var vouchers = await dbContext.Seats
                .Where(w => w.PerformanceId == -1)
                .Where(s => s.OrderItemId != null && s.OrderItem!.Order.BasketId == req.BasketId)
                .ToListAsync(ct);
            vouchers.ForEach(f => f.Row = Guid.NewGuid().ToString());
            await dbContext.SaveChangesAsync(ct);
            await SendOkAsync(new Response(vouchers.Select(s => s.Row).ToList()), ct);
        }
    }
}