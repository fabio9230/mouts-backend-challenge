using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

public sealed class DeleteSaleHandler(
    ISaleRepository repository,
    ILogger<DeleteSaleHandler> logger)
    : IRequestHandler<DeleteSaleCommand>
{
    public async Task Handle(
        DeleteSaleCommand request,
        CancellationToken cancellationToken)
    {
        logger
            .LogInformation(
                "Deleting sale. SaleId={SaleId}",
                request.Id);

        if (!await repository.DeleteAsync(request.Id, cancellationToken))
            throw new KeyNotFoundException($"Sale with ID {request.Id} not found.");

        logger
            .LogInformation(
                "Sale deleted successfully. SaleId={SaleId}",
                request.Id);
    }
}