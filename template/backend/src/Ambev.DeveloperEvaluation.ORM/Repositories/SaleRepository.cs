using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(
        Sale sale,
        CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return sale;
    }

    public async Task<(Sale Sale, bool IsReplay)> CreateIdempotentAsync(
        Sale sale,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var existing = await _context.SaleIdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                throw new IdempotencyConflictException(
                    "The Idempotency-Key was already used with a different request.");

            var existingSale = await GetByIdAsync(existing.SaleId, cancellationToken);
            if (existingSale is null)
                throw new KeyNotFoundException(
                    $"Sale with ID {existing.SaleId} associated with the Idempotency-Key was not found.");

            await transaction.CommitAsync(cancellationToken);

            return (existingSale, true);
        }

        try
        {
            await _context.Sales.AddAsync(sale, cancellationToken);
            await _context.SaleIdempotencyRecords.AddAsync(
                new SaleIdempotencyRecord(idempotencyKey, requestHash, sale.Id),
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (sale, false);
        }
        catch (DbUpdateException ex) when (IsIdempotencyUniqueViolation(ex))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _context.ChangeTracker.Clear();

            var committedRecord = await _context.SaleIdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Key == idempotencyKey, cancellationToken);

            if (committedRecord is null)
                throw;

            if (!string.Equals(committedRecord.RequestHash, requestHash, StringComparison.Ordinal))
                throw new IdempotencyConflictException(
                    "The Idempotency-Key was already used with a different request.");

            var existingSale = await GetByIdAsync(committedRecord.SaleId, cancellationToken);
            if (existingSale is null)
                throw new KeyNotFoundException(
                    $"Sale with ID {committedRecord.SaleId} associated with the Idempotency-Key was not found.");

            return (existingSale, true);
        }
        catch (DbUpdateException ex) when (IsSaleNumberUniqueViolation(ex))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException(
                $"Sale number '{sale.SaleNumber}' already exists.", ex);
        }
    }

    public Task<Sale?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.Sales
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Sale>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SaleNumberExistsAsync(
        string saleNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Sales.AsQueryable().Where(x => x.SaleNumber == saleNumber);

        if (excludingId.HasValue)
            query = query.Where(x => x.Id != excludingId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Sale sale,
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var sale = await _context.Sales
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sale is null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static bool IsIdempotencyUniqueViolation(DbUpdateException exception) =>
        IsUniqueViolation(exception, "IX_SaleIdempotencyRecords_Key");

    private static bool IsSaleNumberUniqueViolation(DbUpdateException exception) =>
        IsUniqueViolation(exception, "IX_Sales_SaleNumber");

    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && string.Equals(postgresException.ConstraintName, constraintName, StringComparison.Ordinal);
}