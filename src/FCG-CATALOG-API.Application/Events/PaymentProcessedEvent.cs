namespace FCG_CATALOG_API.Application.Events;

public enum PaymentStatus { Approved, Rejected }

public record PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }
    public string? Reason { get; init; }
    public DateTime ProcessedAt { get; init; }
}
