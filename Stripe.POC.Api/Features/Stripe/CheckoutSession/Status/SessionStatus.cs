using FastEndpoints;
using Stripe.Checkout;

namespace POC.Api.Features.Stripe.CheckoutSession.Status;

public static class SessionStatus
{
    public record Request(string SessionId);
    public record Response(string Status, string Email);

    public class Endpoint(ILogger<Endpoint> logger) : Endpoint<Request, Response>
    {
        private readonly Lazy<SessionService> _checkoutSessionService = new(() => new SessionService());
        public override void Configure()
        {
            Post("/status");
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
            
            var response = new Response(session.Status, session.CustomerDetails.Email ?? string.Empty);
            logger.LogInformation("Customer email: {Email}", response.Email);
            await SendOkAsync(response, ct);
        }
    }
}