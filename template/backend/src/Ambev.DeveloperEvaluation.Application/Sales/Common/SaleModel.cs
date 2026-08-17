namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public sealed class SaleModel
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<SaleItemModel> Items { get; set; } = [];
}