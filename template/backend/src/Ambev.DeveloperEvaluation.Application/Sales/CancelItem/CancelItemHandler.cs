using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelItem;

public sealed class CancelItemHandler(
    ISaleRepository repository,
    IMapper mapper,
    ISaleEventPublisher eventPublisher,
    ILogger<CancelItemHandler> logger)
    : IRequestHandler<CancelItemCommand, SaleModel>
{
    public async Task<SaleModel> Handle(
        CancelItemCommand request,
        CancellationToken cancellationToken)
    {
        logger
            .LogInformation(
            "Cancelling sale item. SaleId={SaleId}, ItemId={ItemId}",
            request.SaleId,
            request.ItemId);

        var sale = await repository
            .GetByIdAsync(request.SaleId, cancellationToken)
                ?? throw new KeyNotFoundException($"Sale with ID {request.SaleId} not found.");

        if (sale.Status == Domain.Enums.SaleStatus.Cancelled)
            throw new InvalidOperationException("A cancelled sale cannot have its items modified.");

        sale.CancelItem(request.ItemId);
        await repository.UpdateAsync(sale, cancellationToken);

        foreach (var @event in sale.Events)
            await eventPublisher.PublishAsync(@event, cancellationToken);

        logger
            .LogInformation(
                "Sale item cancelled successfully. SaleId={SaleId}, ItemId={ItemId}",
                sale.Id,
                request.ItemId);

        sale.ClearEvents();

        return mapper.Map<SaleModel>(sale);
    }
}
