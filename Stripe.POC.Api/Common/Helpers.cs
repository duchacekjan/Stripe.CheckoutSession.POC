using POC.Api.DTOs;

namespace POC.Api.Common;

public static class Helpers
{
    public static string GetTicketsDescription(this TicketDTO ticket, IEnumerable<TicketDTO> tickets)
        => GetTicketsDescription(ticket.PerformanceId, ticket.PerformanceDate, tickets.Select(s => (s.SeatRow, s.SeatNumber)));


    public static string GetTicketsDescription(long performanceId, DateTime performanceDate, IEnumerable<(string SeatRow, uint SeatNumber)> tickets) =>
        performanceId == -1
            ? string.Join(", ", tickets.Select(s => s.SeatRow))
            : $"Performance date: {performanceDate}\nSeats: {string.Join(", ", tickets.Select(s => $"{s.SeatRow}{s.SeatNumber}"))}";
}