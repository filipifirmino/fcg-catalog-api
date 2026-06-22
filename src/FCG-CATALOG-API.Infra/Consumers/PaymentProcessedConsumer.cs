using FCG_CATALOG_API.Application.Events;
using FCG_CATALOG_API.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FCG_CATALOG_API.Infra.Consumers;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly IAcquisitionService _acquisitionService;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(IAcquisitionService acquisitionService, ILogger<PaymentProcessedConsumer> logger)
    {
        _acquisitionService = acquisitionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var payment = context.Message;

        if (payment.Status == PaymentStatus.Approved)
        {
            await _acquisitionService.AddToLibraryAsync(payment.UserId, payment.GameId, payment.Amount);
            _logger.LogInformation("Game {GameTitle} added to library for user {UserId}.", payment.GameTitle, payment.UserId);
        }
        else
        {
            _logger.LogWarning("Payment rejected for order {OrderId}. Reason: {Reason}", payment.OrderId, payment.Reason);
        }
    }
}
