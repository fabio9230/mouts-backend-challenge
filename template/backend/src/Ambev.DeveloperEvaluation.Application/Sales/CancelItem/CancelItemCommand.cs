using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelItem;

public sealed class CancelItemCommand : IRequest<SaleModel>
{
    public Guid SaleId { get; set; }
    public Guid ItemId { get; set; }
}
