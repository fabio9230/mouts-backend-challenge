using Ambev.DeveloperEvaluation.Application.Sales.Common;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public sealed class CreateSaleResult
{
    public SaleModel Sale { get; init; } = new();
    public bool IsIdempotentReplay { get; init; }
}