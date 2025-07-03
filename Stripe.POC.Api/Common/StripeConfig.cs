namespace POC.Api.Common;

public class StripeConfig
{
    public required string ApiKey { get; set; }
    
    /// <summary>
    /// Url where the user will be redirected after the payment is completed.
    /// </summary>
    /// <remarks>
    /// On real site we will use checkout page where it will consume
    /// some query parameter, it could be that session id from samples, and
    /// displays just payment in progress message (loader) and then waits for
    /// websocket message to redirect to order successful page, based on webhook from Stripe. 
    /// </remarks>
    public required string ReturnUrl { get; set; }
}