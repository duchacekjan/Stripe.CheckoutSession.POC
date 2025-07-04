namespace POC.Api.DTOs;

public record PaidOrdersDTO(long OrderId, Guid BasketId, List<TicketDTO> Tickets, decimal TotalPrice);