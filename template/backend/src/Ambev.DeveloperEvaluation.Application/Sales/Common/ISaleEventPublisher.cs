namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public interface ISaleEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default);
}
