namespace POC.Api.DTOs;

public record SeatListDTO(long Id, string Row, uint Number, long PriceId, decimal Amount, bool IsAvailable);