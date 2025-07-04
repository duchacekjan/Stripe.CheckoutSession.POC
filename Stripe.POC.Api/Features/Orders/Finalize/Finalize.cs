using FastEndpoints;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe.Checkout;

namespace POC.Api.Features.Orders.Finalize;

public static class Finalize
{
    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<EmptyResponse>
    {
        private static readonly string[] ActiveStates = ["open", "pending"];

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

        public override async Task HandleAsync(CancellationToken ct)
        {
            var basketId = Route<Guid>("BasketId");
            var session = await dbContext.CheckoutSessions
                .Where(o => o.Order.BasketId == basketId)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var service = new SessionService();
            var checkoutSession = await service.GetAsync(session.SessionId, cancellationToken: ct);
            if (!ActiveStates.Contains(checkoutSession.Status))
            {
                ValidationFailures.Add(new ValidationFailure(nameof(checkoutSession.Status),
                    $"Cannot finalize order {session.OrderId} with status {checkoutSession.Status}. Only not completed sessions can be finalized."));
                await SendErrorsAsync(cancellation: ct);
                return;
            }

            var payment = await dbContext.Payments
                .Where(p => p.OrderId == session.OrderId)
                .FirstOrDefaultAsync(ct);
            if (payment == null)
            {
                payment = new Payment
                {
                    OrderId = session.OrderId,
                    SessionId = session.SessionId
                };
                dbContext.Payments.Add(payment);
            }

            payment.Status = PaymentStatus.Created;
            await dbContext.SaveChangesAsync(ct);

            // Simulate some processing delay
            //await Task.Delay(5_000, ct); 
            await SendNoContentAsync(ct);
        }
    }
}