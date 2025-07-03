using FastEndpoints;
using POC.Api.Common;

namespace POC.Api.Features.Stripe.CheckoutSession.Create;

public static class CreateSession
{
    public record Request(Guid BasketId);

    public record Response(string ClientSecret, string SessionId);

    public class Endpoint(StripeSessionService stripeSessionService) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/{BasketId}/create");
            Group<CheckoutSessionGroup>();
            Description(d => d
                .Produces<Response>(StatusCodes.Status201Created)
                .Produces<string>(StatusCodes.Status404NotFound)
                .WithName($"{nameof(CheckoutSession)}.{nameof(CreateSession)}")
            );
            Summary(s =>
            {
                s.Summary = "Client secret for created checkout session";
                s.Responses[StatusCodes.Status201Created] = "Successfully created checkout session";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var session = await stripeSessionService.GetOrCreateAsync(req.BasketId, ct);
            if (session is null)
            {
                await SendNotFoundAsync(ct);
                return;
            }
            await SendAsync(new Response(session.ClientSecret, session.SessionId), StatusCodes.Status201Created, ct);
        }
    }
}