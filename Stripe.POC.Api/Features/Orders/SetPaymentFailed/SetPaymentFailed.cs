using FastEndpoints;
using POC.Api.Common;
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
            await dbContext.UpdatePaymentAsync(basketId, PaymentStatus.Failed, ct);
            await SendNoContentAsync(ct);
        }
    }
}