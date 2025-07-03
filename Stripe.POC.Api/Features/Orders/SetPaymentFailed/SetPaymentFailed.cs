using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Orders.SetPaymentFailed;

public static class SetPaymentFailed
{
    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest
    {
        public override void Configure()
        {
            Post("/{BasketId}/set-payment-failed");
            Group<OrdersGroup>();
            Description(d => d
                .Produces(StatusCodes.Status204NoContent)
                .WithName($"{nameof(Orders)}.{nameof(SetPaymentFailed)}")
            );
            Summary(s =>
            {
                s.Summary = "Set payment failed for order";
                s.Responses[StatusCodes.Status204NoContent] = "Payment status updated successfully";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var basketId = Route<Guid>("BasketId");
            var payment = await dbContext.Payments
                .Where(w => w.Order.BasketId == basketId)
                .Where(w => w.Status == PaymentStatus.Created)
                .Where(w => w.UpdatedAt == null)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (payment is not null)
            {
                payment.Status = PaymentStatus.Failed;
                payment.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);
            }

            await SendNoContentAsync(ct);
        }
    }
}