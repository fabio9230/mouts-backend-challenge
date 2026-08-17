namespace Ambev.DeveloperEvaluation.Domain.Entities.Sales;

public sealed class SaleIdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public Guid SaleId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SaleIdempotencyRecord() { }

    public SaleIdempotencyRecord(
        string key,
        string requestHash,
        Guid saleId)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Idempotency key is required.", nameof(key));

        if (string.IsNullOrWhiteSpace(requestHash))
            throw new ArgumentException("Request hash is required.", nameof(requestHash));

        Id = Guid.NewGuid();
        Key = key.Trim();
        RequestHash = requestHash;
        SaleId = saleId;
        CreatedAt = DateTime.UtcNow;
    }
}
