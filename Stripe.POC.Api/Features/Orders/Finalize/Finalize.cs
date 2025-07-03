using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Orders.Finalize;

public static class Finalize
{
    public record Request(Guid BasketId);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Post("/{BasketId}/finalize");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<EmptyResponse>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(Finalize)}")
            );
            Summary(s =>
            {
                s.Summary = "Finalizes the order update process";
                s.Responses[StatusCodes.Status200OK] = "Order finalized successfully";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var session = await dbContext.CheckoutSessions
                .Where(o => o.Order.BasketId == req.BasketId)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var payment = await dbContext.Payments
                .Where(p => p.OrderId == session.OrderId)
                .Where(p => p.Status == PaymentStatus.Created)
                .Where(p => p.UpdatedAt == null)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync(ct);
            if (payment == null)
            {
                payment = new Payment
                {
                    OrderId = session.OrderId,
                    Status = PaymentStatus.Created,
                    CreatedAt = DateTime.UtcNow,
                    SessionId = session.SessionId,
                    PaymentIntentId = session.PaymentIntentId
                };
                dbContext.Payments.Add(payment);
                await dbContext.SaveChangesAsync(ct);
            }

            await SendOkAsync(new EmptyResponse(), ct);
        }
    }
}