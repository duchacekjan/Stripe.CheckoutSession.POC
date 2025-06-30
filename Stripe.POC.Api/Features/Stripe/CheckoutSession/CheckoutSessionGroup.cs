using FastEndpoints;

namespace POC.Api.Features.Stripe.CheckoutSession;

public sealed class CheckoutSessionGroup : SubGroup<StripeGroup>
{
    public CheckoutSessionGroup()
    {
        Configure("checkout-session", c =>
        {
            c.Description(d =>
            {
                d.WithTags("Checkout Session");
            });
        });
    }
}