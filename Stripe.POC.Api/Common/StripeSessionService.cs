using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using POC.Api.DTOs;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Common;

public class StripeSessionService(AppDbContext dbContext, IOptions<StripeConfig> options)
{
    private readonly Lazy<SessionService> _checkoutSessionService = new(() => new SessionService());
    private readonly StripeConfig _stripeConfig = options.Value;

    public record Session(string ClientSecret, string SessionId);

    public async Task<Session?> GetOrCreateAsync(Guid basketId, CancellationToken ct)
    {
        var tickets = await dbContext.OrderTickets2Async(basketId, ct);
        if (tickets.Count == 0)
        {
            return null;
        }

        var session = await GetStoredSessionAsync(basketId, tickets, ct);
        if (session != null)
        {
            return session;
        }

        return await CreateCheckoutSessionAsync(basketId, tickets, ct);
    }

    private async Task<Session?> GetStoredSessionAsync(Guid basketId, Dictionary<long, List<TicketDTO>> tickets, CancellationToken ct)
    {
        var data = await dbContext.CheckoutSessions
            .Where(w => w.Order.BasketId == basketId)
            .Select(s => new { s.ClientSecret, s.SessionId })
            .FirstOrDefaultAsync(ct);

        await UpdateSessionAsync(data?.SessionId, tickets, ct);

        return string.IsNullOrEmpty(data?.ClientSecret) ? null : new Session(data.ClientSecret, data.SessionId);
    }

    public async Task<int> UpdateSessionAsync(string? sessionId, Dictionary<long, List<TicketDTO>> tickets, CancellationToken ct)
    {
        var updatedItems = await GetLineItemsForUpdateAsync(sessionId, tickets, ct);

        if (updatedItems.Count == 0)
        {
            // If there are no items left, we can delete the session
            return 0;
        }

        var updateOptions = new SessionUpdateOptions
        {
            LineItems = updatedItems
        };

        await _checkoutSessionService.Value.UpdateAsync(sessionId, updateOptions, cancellationToken: ct);
        return updatedItems.Count;
    }

    private async Task<List<SessionLineItemOptions>> GetLineItemsForUpdateAsync(string? sessionId, Dictionary<long, List<TicketDTO>> tickets, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return [];
        }

        var lineItems = await _checkoutSessionService.Value.LineItems.ListAsync(sessionId, cancellationToken: ct);
        var updatedItems = lineItems.Select(s => new SessionLineItemOptions
        {
            Id = s.Id,
            Quantity = s.Quantity
        }).ToList();

        var processedOrderItemIds = ProcessExistingOrderItems(lineItems, tickets, updatedItems);
        updatedItems = ProcessNewOrderItems(processedOrderItemIds, tickets, updatedItems);
        return updatedItems;
    }

    private static List<SessionLineItemOptions> ProcessNewOrderItems(List<long> processedOrderItemIds, Dictionary<long, List<TicketDTO>> tickets, List<SessionLineItemOptions> updatedItems)
    {
        var result = updatedItems.ToList();
        foreach (var (orderItemId, orderItemTickets) in tickets.Where(w => !processedOrderItemIds.Contains(w.Key)))
        {
            if (orderItemTickets.Count == 0)
            {
                continue; // Skip if there are no tickets for this order item
            }

            var newItem = orderItemTickets.ToLineItem(orderItemId);
            result.Add(newItem);
        }

        return result;
    }

    private static List<long> ProcessExistingOrderItems(StripeList<LineItem> lineItems, Dictionary<long, List<TicketDTO>> tickets, List<SessionLineItemOptions> updatedItems)
    {
        var processedOrderItemIds = new List<long>();
        foreach (var lineItem in lineItems)
        {
            if (!lineItem.TryGetOrderItemId(out var orderItemId))
            {
                continue;
            }

            processedOrderItemIds.Add(orderItemId);
            var priceBandTickets = tickets[orderItemId];

            if (priceBandTickets.Count == 0)
            {
                // If there are no tickets for this order item, remove the line item
                updatedItems.RemoveAll(item => item.Id == lineItem.Id);
                continue;
            }

            var existingItem = updatedItems.FirstOrDefault(f => f.Id == lineItem.Id);
            if (existingItem != null)
            {
                existingItem.Quantity = priceBandTickets.Count;
            }
        }

        return processedOrderItemIds;
    }

    private async Task<Session> CreateCheckoutSessionAsync(Guid basketId, Dictionary<long, List<TicketDTO>> tickets, CancellationToken ct)
    {
        var lineItems = CreateLineItems(tickets);
        var session = await CreateCheckoutSessionAsync(basketId, lineItems, ct);
        await SaveCheckoutSessionAsync(basketId, session, ct);
        return new Session(session.ClientSecret, session.SessionId);
    }

    private async Task SaveCheckoutSessionAsync(Guid basketId, (string SessionId, string ClientSecret) session, CancellationToken ct)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(f => f.BasketId == basketId, ct);

        if (order == null)
        {
            throw new InvalidOperationException($"Order with basket ID {basketId} not found.");
        }

        order.CheckoutSession = new CheckoutSession
        {
            SessionId = session.SessionId,
            ClientSecret = session.ClientSecret,
            OrderId = order.Id
        };
        await dbContext.SaveChangesAsync(ct);
    }

    private static List<SessionLineItemOptions> CreateLineItems(Dictionary<long, List<TicketDTO>> tickets)
    {
        var items = new List<SessionLineItemOptions>();

        foreach (var (orderItemId, orderItemTickets) in tickets)
        {
            items.Add(orderItemTickets.ToLineItem(orderItemId));
        }

        return items;
    }

    private async Task<(string SessionId, string ClientSecret)> CreateCheckoutSessionAsync(Guid basketId, List<SessionLineItemOptions> items, CancellationToken ct)
    {
        var options = new SessionCreateOptions
        {
            UiMode = "custom",
            Permissions = new SessionPermissionsOptions
            {
                UpdateLineItems = "server_only"
            },
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

        //TODO Setting permissions does not work here. It is unknown value
        //Stripe.StripeException: Received unknown parameter: permissions[update_discounts]. Did you mean update_shipping_details?
        //Tried 48.3.0-beta.1
        //Tried 48.3.0-beta.2
        //Tried 48.4.0-beta.1
        //options.AddExtraParam("permissions[update_discounts]", "server_only");

        var session = await _checkoutSessionService.Value.CreateAsync(options, cancellationToken: ct);
        return (session.Id, session.ClientSecret);
    }
}