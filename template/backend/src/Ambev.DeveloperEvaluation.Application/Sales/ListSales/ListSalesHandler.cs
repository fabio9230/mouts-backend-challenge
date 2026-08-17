using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public sealed class ListSalesHandler(
    ISaleRepository repository,
    IMapper mapper,
    ILogger<ListSalesHandler> logger)
    : IRequestHandler<ListSalesCommand, IReadOnlyCollection<SaleModel>>
{
    public async Task<IReadOnlyCollection<SaleModel>> Handle(
        ListSalesCommand request,
        CancellationToken cancellationToken)
    {
        logger
            .LogInformation("Listing sales.");

        var sales = await repository
            .GetAllAsync(cancellationToken);

        return mapper.Map<IReadOnlyCollection<SaleModel>>(sales);
    }
}