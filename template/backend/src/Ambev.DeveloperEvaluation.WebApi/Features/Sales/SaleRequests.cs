namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

public abstract class SaleRequestBase
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
}

public abstract class SaleItemRequestBase
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class CreateSaleRequest : SaleRequestBase
{
    public List<CreateSaleItemRequest> Items { get; set; } = [];
}

public sealed class CreateSaleItemRequest : SaleItemRequestBase
{
}

public sealed class UpdateSaleRequest : SaleRequestBase
{
    public List<UpdateSaleItemRequest> Items { get; set; } = [];
}

public sealed class UpdateSaleItemRequest : SaleItemRequestBase
{
    public Guid? Id { get; set; }
    public bool IsCancelled { get; set; }
}
