namespace POC.Api.DTOs;

public record EventListDTO(long Id, string Name, List<PerformanceListDTO> Performances);