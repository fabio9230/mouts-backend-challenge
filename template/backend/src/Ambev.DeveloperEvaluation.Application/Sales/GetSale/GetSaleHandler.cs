using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public sealed class GetSaleHandler(
    ISaleRepository repository,
    IMapper mapper,
    ILogger<GetSaleHandler> logger)
    : IRequestHandler<GetSaleCommand, SaleModel>
{
    public async Task<SaleModel> Handle(
        GetSaleCommand request,
        CancellationToken cancellationToken)
    {
        logger
            .LogInformation(
                "Getting sale. SaleId={SaleId}",
                request.Id);

        var sale = await repository
            .GetByIdAsync(request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Sale with ID {request.Id} not found.");

        return mapper.Map<SaleModel>(sale);
    }
}
