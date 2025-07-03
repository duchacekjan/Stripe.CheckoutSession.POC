using FastEndpoints;
using POC.Api.Common;

namespace POC.Api.Features.Stripe.CheckoutSession.Update;

public static class UpdateSession
{
    public record Request(Guid BasketId);

    public class Endpoint(StripeSessionService stripeSessionService) : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Put("/update");
            Group<CheckoutSessionGroup>();
            Description(d => d
                .Produces(StatusCodes.Status204NoContent)
                .WithName($"{nameof(CheckoutSession)}.{nameof(UpdateSession)}")
            );
            Summary(s =>
            {
                s.Summary = "Update checkout session";
                s.Responses[StatusCodes.Status204NoContent] = "Successfully updated checkout session";
            });
        }

        public override async Task HandleAsync(Request request, CancellationToken ct)
        {
            _ = await stripeSessionService.GetOrCreateAsync(request.BasketId, ct);
            await SendNoContentAsync(ct);
        }
    }
}