namespace POC.Api.Common;

public class StripeConfig
{
    public required string ApiKey { get; set; }
    public required string ReturnUrl { get; set; }
}