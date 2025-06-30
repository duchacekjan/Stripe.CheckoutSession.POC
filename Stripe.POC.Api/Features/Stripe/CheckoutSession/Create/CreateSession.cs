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

    public record Response(string ClientSecret);

    public class Endpoint(AppDbContext dbContext, IOptions<StripeConfig> options) : Endpoint<Request, Response>
    {
        private readonly Lazy<SessionService> _checkoutSessionService = new(() => new SessionService());
        private readonly StripeConfig _stripeConfig = options.Value;

        public override void Configure()
        {
            Post("/create");
            Group<CheckoutSessionGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>(StatusCodes.Status201Created)
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
            var (sessionId, clientSecret) = await CreateCheckoutSessionAsync(basketId, lineItems, ct);
            await SaveCheckoutSessionAsync(basketId, sessionId, ct);
            return new Response(clientSecret);
        }

        private async Task<Response?> GetStoredSessionAsync(Guid basketId, CancellationToken ct)
        {
            var storedSessionId = await dbContext.CheckoutSessions
                .Where(w => w.Order.BasketId == basketId)
                .Select(s => s.SessionId)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrEmpty(storedSessionId))
            {
                return null;
            }

            var sessionClientSecret = await GetClientSecretAsync(storedSessionId, ct);
            return
                new Response(sessionClientSecret);
        }

        private async Task SaveCheckoutSessionAsync(Guid basketId, string sessionId, CancellationToken ct)
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(f => f.BasketId == basketId, ct);

            if (order == null)
            {
                throw new InvalidOperationException($"Order with basket ID {basketId} not found.");
            }

            order.CheckoutSession = new Persistence.Entities.CheckoutSession
            {
                SessionId = sessionId,
                OrderId = order.Id
            };
            await dbContext.SaveChangesAsync(ct);
        }

        private static async Task<string> GetClientSecretAsync(string sessionId, CancellationToken ct)
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId, cancellationToken: ct);
            return session.ClientSecret;
        }

        private static List<SessionLineItemOptions> CreateLineItems(List<TicketDTO> tickets)
        {
            var items = new List<SessionLineItemOptions>();

            foreach (var performance in tickets.GroupBy(g => g.PerformanceId))
            {
                items.AddRange(performance.GroupBy(g => g.PriceId).Select(priceBand => CreateItem(priceBand.ToList())));
            }

            return items;
        }

        private static SessionLineItemOptions CreateItem(List<TicketDTO> tickets) => new()
        {
            Quantity = tickets.Count,
            PriceData = CreatePriceData(tickets, tickets.First())
        };

        private static SessionLineItemPriceDataOptions CreatePriceData(List<TicketDTO> tickets, TicketDTO info) => new()
        {
            Currency = "gbp",
            ProductData = CreateProductData(tickets, info),
            UnitAmount = (long)(info.Price * 100)
        };

        private static SessionLineItemPriceDataProductDataOptions CreateProductData(List<TicketDTO> tickets, TicketDTO info) => new()
        {
            Name = info.EventName,
            Description = info.GetTicketsDescription(tickets),
            Metadata = new Dictionary<string, string>
            {
                { "eventId", info.EventId.ToString() },
                { "performanceId", info.PerformanceId.ToString() },
                { "priceId", info.PriceId.ToString() },
            }
        };

        private async Task<(string SessionId, string ClientSecret)> CreateCheckoutSessionAsync(Guid basketId, List<SessionLineItemOptions> items, CancellationToken ct)
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

            var session = await _checkoutSessionService.Value.CreateAsync(options, cancellationToken: ct);
            return (session.Id, session.ClientSecret);
        }
    }
}