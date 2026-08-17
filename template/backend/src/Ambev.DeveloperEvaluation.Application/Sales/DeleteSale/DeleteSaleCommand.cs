using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

public sealed class DeleteSaleCommand : IRequest
{
    public Guid Id { get; set; }
}