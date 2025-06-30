using FastEndpoints;

namespace POC.Api.Features.Stripe;

public sealed class StripeGroup : Group
{
    public StripeGroup()
    {
        Configure("stripe", c =>
        {
            c.Description(d =>
            {
                d.WithTags("Stripe");
            });
        });
    }
}