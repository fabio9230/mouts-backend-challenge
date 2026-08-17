using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public sealed class CreateSaleHandler(
    ISaleRepository repository,
    IDiscountPolicy discountPolicy,
    IMapper mapper,
    ISaleEventPublisher eventPublisher,
    ILogger<CreateSaleHandler> logger)
    : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    public async Task<CreateSaleResult> Handle(
        CreateSaleCommand request,
        CancellationToken cancellationToken)
    {
        logger
            .LogInformation(
                "Creating sale. SaleNumber={SaleNumber}, CustomerId={CustomerId}, BranchId={BranchId}",
                request.SaleNumber,
                request.CustomerId,
                request.BranchId);

        var sale = new Sale(
            request.SaleNumber,
            request.Date,
            request.CustomerId,
            request.BranchId);

        foreach (var item in request.Items)
        {
            sale.AddNewItem(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                discountPolicy);
        }

        var requestHash = CalculateRequestHash(request);

        var (persistedSale, isReplay) = await repository
            .CreateIdempotentAsync(
                sale,
                request.IdempotencyKey,
                requestHash,
                cancellationToken);

        if (isReplay)
        {
            logger
                .LogInformation(
                    "Sale creation replayed. SaleId={SaleId}, SaleNumber={SaleNumber}, IdempotencyKey={IdempotencyKey}",
                    persistedSale.Id,
                    persistedSale.SaleNumber,
                    request.IdempotencyKey);
        }
        else
        {
            foreach (var @event in persistedSale.Events)
                await eventPublisher.PublishAsync(@event, cancellationToken);

            logger
                .LogInformation(
                    "Sale created successfully. SaleId={SaleId}, SaleNumber={SaleNumber}",
                    persistedSale.Id,
                    persistedSale.SaleNumber);

            persistedSale.ClearEvents();
        }

        return new CreateSaleResult
        {
            Sale = mapper.Map<SaleModel>(persistedSale),
            IsIdempotentReplay = isReplay
        };
    }

    private static string CalculateRequestHash(CreateSaleCommand request)
    {
        var payload = new
        {
            request.SaleNumber,
            request.Date,
            request.CustomerId,
            request.BranchId,
            Items = request.Items.Select(item => new
            {
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice
            })
        };

        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(hash);
    }
}
