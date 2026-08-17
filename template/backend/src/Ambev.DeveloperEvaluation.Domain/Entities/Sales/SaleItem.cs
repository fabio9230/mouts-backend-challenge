using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;

namespace Ambev.DeveloperEvaluation.Domain.Entities.Sales;

public sealed class SaleItem
{
    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    private SaleItem() { }

    internal SaleItem(
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice,
        IDiscountPolicy discountPolicy)
    {
        Validate(
            productName,
            quantity,
            unitPrice);

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);

        ApplyDiscount(discountPolicy);
    }

    public void IncreaseQuantity(
        int quantity,
        IDiscountPolicy discountPolicy)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        var newQuantity = Quantity + quantity;
        if (newQuantity > 20)
            throw new DomainException("It is not possible to sell more than 20 identical items.");

        Quantity = newQuantity;
        ApplyDiscount(discountPolicy);
    }

    public void Update(
        string productName,
        int quantity,
        decimal unitPrice,
        IDiscountPolicy discountPolicy)
    {
        Validate(productName, quantity, unitPrice);

        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);

        ApplyDiscount(discountPolicy);
    }

    public void Cancel()
    {
        if (IsCancelled)
            return;

        IsCancelled = true;
        TotalAmount = 0m;
    }

    private void ApplyDiscount(IDiscountPolicy discountPolicy)
    {
        DiscountPercentage = discountPolicy
            .CalculateDiscountPercentage(Quantity) * 100m;

        DiscountAmount = discountPolicy
            .CalculateDiscountAmount(Quantity, UnitPrice);

        TotalAmount = decimal.Round(
            Quantity * UnitPrice - DiscountAmount,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static void Validate(string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        if (quantity > 20)
            throw new DomainException("It is not possible to sell more than 20 identical items.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");
    }
}
