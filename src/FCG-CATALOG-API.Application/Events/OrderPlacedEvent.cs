namespace FCG_CATALOG_API.Application.Events;

public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime PlacedAt { get; init; }
}
