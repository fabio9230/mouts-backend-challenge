using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.IoC;

public sealed class LogSaleEventPublisher(
    ILogger<LogSaleEventPublisher> logger)
    : ISaleEventPublisher
{
    public Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sale event published. EventType: {EventType}, Payload: {@Event}",
            typeof(TEvent).Name,
            @event);

        return Task.CompletedTask;
    }
}
