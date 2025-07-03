using FastEndpoints;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe.FinancialConnections;

namespace POC.Api.Features.Orders.Finalize;

public static class Finalize
{
    public record Request(Guid BasketId);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, EmptyResponse>
    {
        private static string[] _activeStates = ["open", "pending"];

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

            var service = new SessionService();
            var checkoutSession = await service.GetAsync(session.SessionId, cancellationToken: ct);
            if (!_activeStates.Contains(checkoutSession.Status))
            {
                ValidationFailures.Add(new ValidationFailure(nameof(checkoutSession.Status),
                    $"Cannot finalize order {session.OrderId} with status {checkoutSession.Status}. Only not completed sessions can be finalized."));
                await SendErrorsAsync(cancellation: ct);
                return;
            }
            
            //TODO How to retrieve the payment intent?
            //docs says https://docs.stripe.com/api/checkout/sessions/retrieve?api-version=2025-06-30.preview&lang=dotnet
            //but Session returned does not have PaymentIntent nor PaymentIntentId

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
                    SessionId = session.SessionId
                };
                dbContext.Payments.Add(payment);
                await dbContext.SaveChangesAsync(ct);
            }

            await Task.Delay(5_000, ct); // Simulate some processing delay
            await SendOkAsync(new EmptyResponse(), ct);
        }
    }
}