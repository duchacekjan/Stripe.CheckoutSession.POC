using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using Stripe.Checkout;

namespace POC.Api.Features.Stripe.CheckoutSession.Status;

public static class SessionStatus
{
    public record Request(string SessionId);

    public record Response(string Status, string? Email, string? BasketId);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
    {
        private readonly Lazy<SessionService> _checkoutSessionService = new(() => new SessionService());

        public override void Configure()
        {
            Get("/{SessionId}/status");
            Group<CheckoutSessionGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(CheckoutSession)}.{nameof(SessionStatus)}")
            );
            Summary(s =>
            {
                s.Summary = "Status of given checkout session";
                s.Responses[StatusCodes.Status200OK] = "Successfully created checkout session";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var session = await _checkoutSessionService.Value.GetAsync(req.SessionId, cancellationToken: ct);
            if (session is null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var basketId = await dbContext.CheckoutSessions
                .Where(s => s.SessionId == session.Id)
                .Select(s => (Guid?)s.Order.BasketId)
                .FirstOrDefaultAsync(ct);

            var response = new Response(session.Status, session.CustomerDetails.Email, basketId?.ToString());
            await SendOkAsync(response, ct);
        }
    }
}