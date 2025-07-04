using FastEndpoints;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Features.Orders.Refund;

public static class Refund
{
    public record Request(Guid BasketId, decimal RefundedAmount);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Post("/{basketId}/refund");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(Refund)}")
            );
            Summary(s =>
            {
                s.Summary = "Processes a refund for an order";
                s.Responses[StatusCodes.Status200OK] = "Refund processed successfully";
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

            var sessionId = await dbContext.CheckoutSessions
                .Where(s => s.OrderId == order.Id)
                .Select(s => s.SessionId)
                .FirstAsync(ct);

            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: ct);
            if (session.Status != "complete" || session.PaymentStatus != "paid")
            {
                ValidationFailures.Add(new ValidationFailure(nameof(session.Status),
                    $"Cannot process refund for order {order.Id} with status {session.Status}. Only completed sessions can be refunded."));
                
                await SendErrorsAsync(cancellation: ct);
            }

            var refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = session.PaymentIntentId,
                Amount = (long)(req.RefundedAmount * 100), // Convert to cents
                Reason = RefundReasons.RequestedByCustomer,
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", order.Id.ToString() },
                    { "basketId", order.BasketId.ToString() }
                }
            };

            await refundService.CreateAsync(options, cancellationToken: ct);

            order.Status = OrderStatus.Refunded;
            await dbContext.SaveChangesAsync(ct);

            await SendOkAsync(new EmptyResponse(), ct);
        }
    }
}