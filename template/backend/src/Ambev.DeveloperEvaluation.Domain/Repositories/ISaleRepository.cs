using Ambev.DeveloperEvaluation.Domain.Entities.Sales;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Repository interface for Sale entity operations
/// </summary>
public interface ISaleRepository
{
    Task<Sale> CreateAsync(
        Sale sale,
        CancellationToken cancellationToken = default);

    Task<(Sale Sale, bool IsReplay)> CreateIdempotentAsync(
        Sale sale,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default);

    Task<Sale?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Sale>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SaleNumberExistsAsync(
        string saleNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Sale sale,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
