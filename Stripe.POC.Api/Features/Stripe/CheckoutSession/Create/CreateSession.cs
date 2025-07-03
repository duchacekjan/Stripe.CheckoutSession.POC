using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using POC.Api.Common;
using POC.Api.DTOs;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;
using Stripe.Checkout;

namespace POC.Api.Features.Stripe.CheckoutSession.Create;

public static class CreateSession
{
    public record Request(Guid BasketId);

    public record Response(string ClientSecret, string SessionId);

    public class Endpoint(AppDbContext dbContext, IOptions<StripeConfig> options) : Endpoint<Request, Response>
    {
        private readonly Lazy<SessionService> _checkoutSessionService = new(() => new SessionService());
        private readonly StripeConfig _stripeConfig = options.Value;

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
            var response = await GetStoredSessionAsync(req.BasketId, ct);
            if (response != null)
            {
                await SendAsync(response, StatusCodes.Status201Created, ct);
                return;
            }

            var tickets = await dbContext.OrderTicketsAsync(req.BasketId, ct);
            if (tickets.Count == 0)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            response = await CreateCheckoutSessionAsync(req.BasketId, tickets, ct);
            await SendAsync(response, StatusCodes.Status201Created, ct);
        }

        private async Task<Response> CreateCheckoutSessionAsync(Guid basketId, List<TicketDTO> tickets, CancellationToken ct)
        {
            var lineItems = CreateLineItems(tickets);
            var session = await CreateCheckoutSessionAsync(basketId, lineItems, ct);
            await SaveCheckoutSessionAsync(basketId, session, ct);
            return new Response(session.ClientSecret, session.SessionId);
        }

        private async Task<Response?> GetStoredSessionAsync(Guid basketId, CancellationToken ct)
        {
            var data = await dbContext.CheckoutSessions
                .Where(w => w.Order.BasketId == basketId)
                .Select(s => new { s.ClientSecret, s.SessionId })
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrEmpty(data?.ClientSecret) ? null : new Response(data.ClientSecret, data.SessionId);
        }

        private async Task SaveCheckoutSessionAsync(Guid basketId, (string SessionId, string ClientSecret, string PaymentIntentId) session, CancellationToken ct)
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(f => f.BasketId == basketId, ct);

            if (order == null)
            {
                throw new InvalidOperationException($"Order with basket ID {basketId} not found.");
            }

            order.CheckoutSession = new Persistence.Entities.CheckoutSession
            {
                SessionId = session.SessionId,
                ClientSecret = session.ClientSecret,
                PaymentIntentId = session.PaymentIntentId,
                OrderId = order.Id
            };
            await dbContext.SaveChangesAsync(ct);
        }

        private static List<SessionLineItemOptions> CreateLineItems(List<TicketDTO> tickets)
        {
            var items = new List<SessionLineItemOptions>();

            foreach (var performance in tickets.GroupBy(g => g.PerformanceId))
            {
                items.AddRange(performance.GroupBy(g => g.PriceId).Select(priceBand => priceBand.ToLineItem()));
            }

            return items;
        }

        private async Task<(string SessionId, string ClientSecret, string PaymentIntentId)> CreateCheckoutSessionAsync(Guid basketId, List<SessionLineItemOptions> items, CancellationToken ct)
        {
            var options = new SessionCreateOptions
            {
                UiMode = "custom",
                Permissions = new SessionPermissionsOptions(),
                Mode = "payment",
                ReturnUrl = _stripeConfig.ReturnUrl,
                AdaptivePricing = new SessionAdaptivePricingOptions { Enabled = true },
                LineItems = items,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "basketId", basketId.ToString() }
                    }
                }
            };

            options.AddExtraParam("permissions[update_line_items]", "server_only");
            options.AddExtraParam("permissions[update_discounts]", "server_only");

            var session = await _checkoutSessionService.Value.CreateAsync(options, cancellationToken: ct);
            return (session.Id, session.ClientSecret, session.PaymentIntentId);
        }
    }
}