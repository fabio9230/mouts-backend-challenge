using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;

namespace Ambev.DeveloperEvaluation.Domain.Entities.Sales;

public sealed class Sale : BaseEntity
{
    public string SaleNumber { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid BranchId { get; private set; }
    public SaleStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Where(x => !x.IsCancelled).Sum(x => x.TotalAmount);
    public IReadOnlyCollection<ISaleEvent> Events => _events.AsReadOnly();
    private readonly List<ISaleEvent> _events = [];
    private readonly List<SaleItem> _items = [];

    public Sale(
        string saleNumber,
        DateTime date,
        Guid customerId,
        Guid branchId)
    {
        ValidateHeader(
            saleNumber,
            customerId,
            branchId);

        Id = Guid.NewGuid();
        SaleNumber = saleNumber.Trim();
        Date = date;
        CustomerId = customerId;
        BranchId = branchId;
        Status = SaleStatus.Active;

        AddEvent(new
            SaleCreatedEvent(
                Id,
                SaleNumber,
                DateTime.UtcNow));
    }

    public void UpdateHeader(
        string saleNumber,
        DateTime date,
        Guid customerId,
        Guid branchId)
    {
        ValidateHeader(
            saleNumber,
            customerId,
            branchId);

        SaleNumber = saleNumber.Trim();
        Date = date;
        CustomerId = customerId;
        BranchId = branchId;
        UpdatedAt = DateTime.UtcNow;
    }

    public SaleItem AddNewItem(
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice,
        IDiscountPolicy discountPolicy)
    {
        var existingItem = _items
            .FirstOrDefault(x =>
                x.ProductId == productId &&
                !x.IsCancelled);

        if (existingItem is not null)
        {
            if (existingItem.UnitPrice != decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero))
                throw new DomainException("The same product cannot be added with a different unit price.");

            existingItem.IncreaseQuantity(quantity, discountPolicy);
            UpdatedAt = DateTime.UtcNow;

            AddEvent(new
                SaleModifiedEvent(
                    Id,
                    SaleNumber,
                    DateTime.UtcNow));

            return existingItem;
        }

        var item = new SaleItem(
                productId,
                productName,
                quantity,
                unitPrice,
                discountPolicy);

        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;

        AddEvent(new
            SaleModifiedEvent(
                Id,
                SaleNumber,
                DateTime.UtcNow));

        return item;
    }

    public void UpdateItem(
        Guid itemId,
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice,
        IDiscountPolicy discountPolicy)
    {
        var item = GetItem(itemId);

        if (_items.Any(x => x.Id != itemId && x.ProductId == productId && !x.IsCancelled))
            throw new DomainException("The same product cannot be added more than once to the sale.");

        item.Update(productName, quantity, unitPrice, discountPolicy);
        UpdatedAt = DateTime.UtcNow;
    }

    public void CancelItem(Guid itemId)
    {
        var item = GetItem(itemId);
        item.Cancel();
        UpdatedAt = DateTime.UtcNow;

        AddEvent(new
            ItemCancelledEvent(
                Id,
                item.Id,
                DateTime.UtcNow));
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            return;

        Status = SaleStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        AddEvent(new
            SaleCancelledEvent(
                Id,
                SaleNumber,
                DateTime.UtcNow));
    }

    public SaleItem GetItem(Guid itemId) => _items
        .FirstOrDefault(x => x.Id == itemId) ??
            throw new KeyNotFoundException($"Sale item with ID {itemId} not found.");

    public void ClearEvents() => _events.Clear();

    public void MarkAsModified()
    {
        AddEvent(
            new SaleModifiedEvent(
                Id,
                SaleNumber,
                DateTime.UtcNow));
    }

    private void AddEvent(ISaleEvent @event) => _events.Add(@event);

    private static void ValidateHeader(
        string saleNumber,
        Guid customerId,
        Guid branchId)
    {
        if (string.IsNullOrWhiteSpace(saleNumber))
            throw new DomainException("Sale number is required.");

        if (customerId == Guid.Empty)
            throw new DomainException("Customer is required.");

        if (branchId == Guid.Empty)
            throw new DomainException("Branch is required.");
    }
}
