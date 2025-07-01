using POC.Api.DTOs;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Common;

public static class Helpers
{
    public static List<TicketDTO> MatchingTickets(this LineItem lineItem, IEnumerable<TicketDTO> tickets)
    {
        var (hasMetadata, eventId, performanceId, priceId) = lineItem.GetLineItemInfo();
        if (!hasMetadata)
            return [];

        return tickets
            .Where(w => w.EventId == eventId && w.PerformanceId == performanceId && w.PriceId == priceId)
            .ToList();
    }

    public static LineItem? FindLineItem(this IEnumerable<LineItem> lineItems, long performanceId, long priceId)
    {
        return lineItems.FirstOrDefault(lineItem =>
        {
            var item = lineItem.GetLineItemInfo();
            return item.HasMetadata && item.PerformanceId == performanceId && item.PriceId == priceId;
        });
    }

    public static SessionLineItemOptions ToLineItem(this IEnumerable<TicketDTO> tickets)
    {
        var ticketsList = tickets.ToList();
        var info = ticketsList.First();

        return new SessionLineItemOptions
        {
            Quantity = ticketsList.Count,
            PriceData = CreatePriceData(ticketsList, info),
            Metadata = CreateMetadata(info)
        };
    }

    private static string GetTicketsDescription(this TicketDTO ticket, IEnumerable<TicketDTO> tickets)
        => GetTicketsDescription(ticket.PerformanceId, ticket.PerformanceDate, tickets.Select(s => (s.SeatRow, s.SeatNumber)));

    private static string GetTicketsDescription(long performanceId, DateTime performanceDate, IEnumerable<(string SeatRow, uint SeatNumber)> tickets) =>
        performanceId > 0
            ? $"Performance date: {performanceDate}\nSeats: {string.Join(", ", tickets.Select(s => $"{s.SeatRow}{s.SeatNumber}"))}"
            : string.Join(", ", tickets.Select(s => s.SeatRow));

    private static (bool HasMetadata, long EventId, long PerformanceId, long PriceId) GetLineItemInfo(this LineItem lineItem)
    {
        var hasMetadata = lineItem.Metadata.TryGetValue("eventId", out var eventId)
                          & lineItem.Metadata.TryGetValue("performanceId", out var performanceId)
                          & lineItem.Metadata.TryGetValue("priceId", out var priceId);
        return (
            hasMetadata,
            eventId != null ? long.Parse(eventId) : -1,
            performanceId != null ? long.Parse(performanceId) : -1,
            priceId != null ? long.Parse(priceId) : -1
        );
    }

    private static Dictionary<string, string> CreateMetadata(TicketDTO info) => new()
    {
        { "eventId", info.EventId.ToString() },
        { "performanceId", info.PerformanceId.ToString() },
        { "priceId", info.PriceId.ToString() },
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
        Metadata = CreateMetadata(info)
    };
}