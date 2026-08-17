using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.Sales;

public sealed class SaleItemTests
{
    private readonly IDiscountPolicy _discountPolicy =
        new ProgressiveDiscountPolicy();

    [Theory(DisplayName = "Should apply correct discount tier")]
    [InlineData(1, 100, 0, 0, 100)]
    [InlineData(3, 100, 0, 0, 300)]
    [InlineData(4, 100, 10, 40, 360)]
    [InlineData(9, 100, 10, 90, 810)]
    [InlineData(10, 100, 20, 200, 800)]
    [InlineData(20, 100, 20, 400, 1600)]
    public void AddNewItem_Should_Apply_Correct_Discount_Tier(
        int quantity,
        decimal unitPrice,
        decimal expectedPercentage,
        decimal expectedDiscount,
        decimal expectedTotal)
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            quantity,
            unitPrice,
            _discountPolicy);

        item.DiscountPercentage.Should().Be(expectedPercentage);
        item.DiscountAmount.Should().Be(expectedDiscount);
        item.TotalAmount.Should().Be(expectedTotal);
    }

    [Theory(DisplayName = "Should reject a non positive quantity")]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddNewItem_Should_Reject_NonPositive_Quantity(int quantity)
    {
        var sale = CreateSale();

        var act = () => sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            quantity,
            100m,
            _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Quantity must be greater than zero.");
    }

    [Fact(DisplayName = "Should reject a quantity above twenty")]
    public void AddNewItem_Should_Reject_Quantity_Above_Twenty()
    {
        var sale = CreateSale();

        var act = () => sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            21,
            100m,
            _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage(
                "It is not possible to sell more than 20 identical items.");
    }

    [Theory(DisplayName = "Should reject a invalid product name")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddNewItem_Should_Reject_Invalid_Product_Name(
        string? productName)
    {
        var sale = CreateSale();

        var act = () => sale.AddNewItem(
            Guid.NewGuid(),
            productName!,
            1,
            100m,
            _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Product name is required.");
    }

    [Theory(DisplayName = "Should reject a non positive unit price")]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void AddNewItem_Should_Reject_NonPositive_Unit_Price(
        decimal unitPrice)
    {
        var sale = CreateSale();

        var act = () => sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            1,
            unitPrice,
            _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Unit price must be greater than zero.");
    }

    [Fact(DisplayName = "Should normalize a product anme and round unit price")]
    public void AddNewItem_Should_Normalize_Product_Name_And_Round_Unit_Price()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "  Product  ",
            1,
            10.126m,
            _discountPolicy);

        item.ProductName.Should().Be("Product");
        item.UnitPrice.Should().Be(10.13m);
        item.TotalAmount.Should().Be(10.13m);
    }

    [Fact(DisplayName = "Should recalculate discount")]
    public void IncreaseQuantity_Should_Recalculate_Discount()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            3,
            100m,
            _discountPolicy);

        item.IncreaseQuantity(7, _discountPolicy);

        item.Quantity.Should().Be(10);
        item.DiscountPercentage.Should().Be(20m);
        item.DiscountAmount.Should().Be(200m);
        item.TotalAmount.Should().Be(800m);
    }

    [Fact(DisplayName = "Should reject non positive quantity")]
    public void IncreaseQuantity_Should_Reject_NonPositive_Quantity()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            5,
            100m,
            _discountPolicy);

        var act = () => item.IncreaseQuantity(0, _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Quantity must be greater than zero.");
    }

    [Fact(DisplayName = "Should reject when new total exceeds the limit of twenty")]
    public void IncreaseQuantity_Should_Reject_When_New_Total_Exceeds_Twenty_And_Keep_State()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            18,
            100m,
            _discountPolicy);

        var act = () => item.IncreaseQuantity(3, _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage(
                "It is not possible to sell more than 20 identical items.");

        item.Quantity.Should().Be(18);
        item.DiscountPercentage.Should().Be(20m);
        item.TotalAmount.Should().Be(1440m);
    }

    [Fact(DisplayName = "Should allow when new total is twenty")]
    public void IncreaseQuantity_Should_Allow_When_New_Total_Is_Twenty()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            18,
            100m,
            _discountPolicy);

        item.IncreaseQuantity(2, _discountPolicy);

        item.Quantity.Should().Be(20);
        item.DiscountPercentage.Should().Be(20m);
        item.TotalAmount.Should().Be(1600m);
    }

    [Fact(DisplayName = "Should change data and recalculate discount")]
    public void Update_Should_Change_Data_And_Recalculate_Discount()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Old",
            2,
            100m,
            _discountPolicy);

        item.Update(
            " New ",
            10,
            50.126m,
            _discountPolicy);

        item.ProductName.Should().Be("New");
        item.Quantity.Should().Be(10);
        item.UnitPrice.Should().Be(50.13m);
        item.DiscountPercentage.Should().Be(20m);
        item.DiscountAmount.Should().Be(100.26m);
        item.TotalAmount.Should().Be(401.04m);
    }

    [Fact(DisplayName = "Should reject invalid data without changing state")]
    public void Update_Should_Reject_Invalid_Data_Without_Changing_State()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            2,
            100m,
            _discountPolicy);

        var act = () => item.Update(
            "",
            5,
            100m,
            _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Product name is required.");

        item.ProductName.Should().Be("Product");
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(100m);
        item.TotalAmount.Should().Be(200m);
    }

    [Fact(DisplayName = "Should mark item a set total to zero")]
    public void Cancel_Should_Mark_Item_And_Set_Total_To_Zero()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            4,
            100m,
            _discountPolicy);

        item.Cancel();

        item.IsCancelled.Should().BeTrue();
        item.TotalAmount.Should().Be(0m);
    }

    [Fact(DisplayName = "Should be idempotent whe try cancel twice")]
    public void Cancel_Should_Be_Idempotent()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            4,
            100m,
            _discountPolicy);

        item.Cancel();
        item.Cancel();

        item.IsCancelled.Should().BeTrue();
        item.TotalAmount.Should().Be(0m);
    }

    private static Sale CreateSale() =>
        new(
            "SALE-ITEM-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());
}