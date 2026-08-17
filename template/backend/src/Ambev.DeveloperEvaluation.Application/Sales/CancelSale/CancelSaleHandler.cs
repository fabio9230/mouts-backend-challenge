using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public sealed class CancelSaleHandler(
    ISaleRepository repository,
    IMapper mapper,
    ISaleEventPublisher eventPublisher,
    ILogger<CancelSaleHandler> logger)
    : IRequestHandler<CancelSaleCommand, SaleModel>
{
    public async Task<SaleModel> Handle(
        CancelSaleCommand request,
        CancellationToken cancellationToken)
    {
        logger
            .LogInformation(
                "Cancelling sale. SaleId={SaleId}",
                request.Id);

        var sale = await repository
                .GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new KeyNotFoundException($"Sale with ID {request.Id} not found.");

        sale.Cancel();
        await repository.UpdateAsync(sale, cancellationToken);

        foreach (var @event in sale.Events)
            await eventPublisher.PublishAsync(@event, cancellationToken);

        logger
            .LogInformation(
                "Sale cancelled successfully. SaleId={SaleId}, SaleNumber={SaleNumber}",
                sale.Id,
                sale.SaleNumber);

        sale.ClearEvents();

        return mapper.Map<SaleModel>(sale);
    }
}
