using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Common;

public static class Helpers
{
    private const string Currency = "gbp";
    private const string PerformanceIdKey = "performanceId";
    private const string PriceIdKey = "priceId";
    private const string EventIdKey = "eventId";
    private const string OrderItemIdKey = "orderItemId";

    public static SessionLineItemOptions ToLineItem(this IEnumerable<TicketDTO> tickets, long orderItemId)
    {
        var ticketsList = tickets.ToList();
        var info = ticketsList.First();

        return new SessionLineItemOptions
        {
            Quantity = ticketsList.Count,
            PriceData = CreatePriceData(ticketsList, info, orderItemId),
            Metadata = CreateMetadata(info, orderItemId)
        };
    }

    public static bool TryGetOrderItemId(this LineItem lineItem, out long orderItemId)
    {
        orderItemId = 0;
        return lineItem.Metadata.TryGetValue(OrderItemIdKey, out var orderItemIdValue) && long.TryParse(orderItemIdValue, out orderItemId);
    }

    public static async Task UpdatePaymentAsync(this AppDbContext dbContext, Guid basketId, PaymentStatus status, CancellationToken ct)
    {
        var payment = await dbContext.Payments
            .Where(w => w.Order.BasketId == basketId)
            .FirstOrDefaultAsync(ct);
        if (payment is null)
        {
            return;
        }

        payment.Status = status;
        payment.PaymentIntentId = await dbContext.GetPaymentIntentIdAsync(basketId, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task<string?> GetPaymentIntentIdAsync(this AppDbContext dbContext, Guid basketId, CancellationToken ct)
    {
        var sessionId = await dbContext.CheckoutSessions
            .Where(o => o.Order.BasketId == basketId)
            .Select(s => s.SessionId)
            .FirstOrDefaultAsync(ct);
        if (sessionId is null)
        {
            return null;
        }

        var service = new SessionService();
        var checkoutSession = await service.GetAsync(sessionId, cancellationToken: ct);
        return checkoutSession.PaymentIntentId;
    }

    private static string GetTicketsDescription(this TicketDTO ticket, IEnumerable<TicketDTO> tickets)
        => GetTicketsDescription(ticket.PerformanceId, ticket.PerformanceDate, tickets.Select(s => (s.SeatRow, s.SeatNumber)));

    private static string GetTicketsDescription(long performanceId, DateTime performanceDate, IEnumerable<(string SeatRow, uint SeatNumber)> tickets) =>
        performanceId > 0
            ? $"Performance date: {performanceDate}\nSeats: {string.Join(", ", tickets.Select(s => $"{s.SeatRow}{s.SeatNumber}"))}"
            : string.Join(", ", tickets.Select(s => s.SeatRow));

    private static Dictionary<string, string> CreateMetadata(TicketDTO info, long orderItemId) => new()
    {
        { EventIdKey, info.EventId.ToString() },
        { PerformanceIdKey, info.PerformanceId.ToString() },
        { PriceIdKey, info.PriceId.ToString() },
        { OrderItemIdKey, orderItemId.ToString() }
    };

    private static SessionLineItemPriceDataOptions CreatePriceData(List<TicketDTO> tickets, TicketDTO info, long orderItemId) => new()
    {
        Currency = Currency,
        ProductData = CreateProductData(tickets, info, orderItemId),
        UnitAmount = (long)(info.Price * 100)
    };

    private static SessionLineItemPriceDataProductDataOptions CreateProductData(List<TicketDTO> tickets, TicketDTO info, long orderItemId) => new()
    {
        Name = info.EventName,
        Description = info.GetTicketsDescription(tickets),
        Metadata = CreateMetadata(info, orderItemId)
    };
}