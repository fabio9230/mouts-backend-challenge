namespace Ambev.DeveloperEvaluation.Domain.Events;

public interface ISaleEvent
{
    Guid SaleId { get; }
    DateTime OccurredAt { get; }
}

public sealed record SaleCreatedEvent(
    Guid SaleId,
    string SaleNumber,
    DateTime OccurredAt)
    : ISaleEvent;

public sealed record SaleModifiedEvent(
    Guid SaleId,
    string SaleNumber,
    DateTime OccurredAt)
    : ISaleEvent;

public sealed record SaleCancelledEvent(
    Guid SaleId,
    string SaleNumber,
    DateTime OccurredAt)
    : ISaleEvent;

public sealed record ItemCancelledEvent(
    Guid SaleId,
    Guid ItemId,
    DateTime OccurredAt)
    : ISaleEvent;
