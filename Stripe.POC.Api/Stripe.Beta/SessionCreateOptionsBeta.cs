using Stripe.Checkout;

namespace POC.Api.Stripe.Beta;

public class SessionCreateOptionsBeta : SessionCreateOptions
{
    public SessionCreateOptionsBeta()
    {
        AddExtraParam("permissions[update_discounts]", "server_only");
    }
}