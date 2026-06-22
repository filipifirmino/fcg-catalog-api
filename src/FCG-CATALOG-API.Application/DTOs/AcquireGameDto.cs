namespace FCG_CATALOG_API.Application.DTOs;

public class AcquireGameDto
{
    public required Guid GameId { get; set; }
    public Guid UserId { get; set; }
}
