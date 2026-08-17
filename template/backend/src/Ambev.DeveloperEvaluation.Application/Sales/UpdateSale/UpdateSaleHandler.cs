using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public sealed class UpdateSaleHandler(
    ISaleRepository repository,
    IDiscountPolicy discountPolicy,
    IMapper mapper,
    ISaleEventPublisher eventPublisher,
    ILogger<UpdateSaleHandler> logger)
    : IRequestHandler<UpdateSaleCommand, SaleModel>
{
    public async Task<SaleModel> Handle(
        UpdateSaleCommand request,
        CancellationToken cancellationToken)
    {
        logger
            .LogInformation(
                "Updating sale. SaleId={SaleId}, SaleNumber={SaleNumber}",
                request.Id,
                request.SaleNumber);

        var sale = await repository
                .GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new KeyNotFoundException($"Sale with ID {request.Id} not found.");

        if (await repository.SaleNumberExistsAsync(request.SaleNumber, request.Id, cancellationToken))
            throw new InvalidOperationException($"Sale number '{request.SaleNumber}' already exists.");

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("A cancelled sale cannot be modified.");

        sale.UpdateHeader(
            request.SaleNumber,
            request.Date,
            request.CustomerId,
            request.BranchId);

        var requestedIds = request.Items
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        foreach (var existing in sale.Items.Where(x => !x.IsCancelled).ToList())
        {
            if (!requestedIds.Contains(existing.Id))
                sale.CancelItem(existing.Id);
        }

        foreach (var item in request.Items)
        {
            if (item.Id.HasValue)
            {
                sale.UpdateItem(
                    item.Id.Value,
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    discountPolicy);

                if (item.IsCancelled)
                    sale.CancelItem(item.Id.Value);
            }
            else
            {
                var created = sale
                    .AddNewItem(
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        discountPolicy);

                if (item.IsCancelled)
                    sale.CancelItem(created.Id);
            }
        }

        sale.MarkAsModified();

        await repository
            .UpdateAsync(
                sale,
                cancellationToken);

        foreach (var @event in sale.Events)
            await eventPublisher.PublishAsync(@event, cancellationToken);

        logger
            .LogInformation(
                "Sale updated successfully. SaleId={SaleId}, SaleNumber={SaleNumber}",
                sale.Id,
                sale.SaleNumber);

        sale.ClearEvents();

        return mapper.Map<SaleModel>(sale);
    }
}
