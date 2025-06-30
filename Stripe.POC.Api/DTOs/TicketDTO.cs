namespace POC.Api.DTOs;

public record TicketDTO(long EventId, string EventName, long PerformanceId, DateTime PerformanceDate, long PriceId, decimal Price, long SeatId, string SeatRow, uint SeatNumber);