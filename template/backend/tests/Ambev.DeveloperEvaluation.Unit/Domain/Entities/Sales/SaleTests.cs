using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.Sales;

public sealed class SaleTests
{
    private readonly IDiscountPolicy _discountPolicy =
        new ProgressiveDiscountPolicy();

    [Fact]
    public void Constructor_Should_Create_Active_Sale_With_Normalized_Number_And_Created_Event()
    {
        var date = DateTime.UtcNow.AddMinutes(-5);
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var sale = new Sale(
            "  SALE-001  ",
            date,
            customerId,
            branchId);

        sale.Id.Should().NotBe(Guid.Empty);
        sale.SaleNumber.Should().Be("SALE-001");
        sale.Date.Should().Be(date);
        sale.CustomerId.Should().Be(customerId);
        sale.BranchId.Should().Be(branchId);
        sale.Status.Should().Be(SaleStatus.Active);
        sale.CreatedAt.Should().BeCloseTo(
            DateTime.UtcNow,
            TimeSpan.FromSeconds(2));
        sale.UpdatedAt.Should().BeNull();
        sale.Items.Should().BeEmpty();
        sale.TotalAmount.Should().Be(0m);

        sale.Events
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<SaleCreatedEvent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Reject_Invalid_Sale_Number(
        string? saleNumber)
    {
        var act = () => new Sale(
            saleNumber!,
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Sale number is required.");
    }

    [Fact]
    public void Constructor_Should_Reject_Empty_Customer()
    {
        var act = () => new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.Empty,
            Guid.NewGuid());

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Customer is required.");
    }

    [Fact]
    public void Constructor_Should_Reject_Empty_Branch()
    {
        var act = () => new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.Empty);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage("Branch is required.");
    }

    [Fact]
    public void UpdateHeader_Should_Update_All_Fields()
    {
        var sale = CreateSale();
        sale.ClearEvents();

        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddHours(-1);

        sale.UpdateHeader(
            "  SALE-002  ",
            date,
            customerId,
            branchId);

        sale.SaleNumber.Should().Be("SALE-002");
        sale.Date.Should().Be(date);
        sale.CustomerId.Should().Be(customerId);
        sale.BranchId.Should().Be(branchId);
        sale.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddNewItem_Should_Create_Item_And_Update_Total()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            " Mouse ",
            4,
            100m,
            _discountPolicy);

        item.ProductName.Should().Be("Mouse");
        sale.Items.Should().ContainSingle();
        sale.TotalAmount.Should().Be(360m);
        sale.UpdatedAt.Should().NotBeNull();
        sale.Events.Should().Contain(x => x is SaleModifiedEvent);
    }

    [Fact]
    public void AddNewItem_Should_Consolidate_Same_Active_Product()
    {
        var sale = CreateSale();
        var productId = Guid.NewGuid();

        var first = sale.AddNewItem(
            productId,
            "Mouse",
            3,
            100m,
            _discountPolicy);

        sale.ClearEvents();

        var result = sale.AddNewItem(
            productId,
            "Mouse",
            1,
            100m,
            _discountPolicy);

        result.Id.Should().Be(first.Id);
        sale.Items.Should().ContainSingle();
        result.Quantity.Should().Be(4);
        result.DiscountPercentage.Should().Be(10m);
        result.TotalAmount.Should().Be(360m);
        sale.Events.Should().ContainSingle(x => x is SaleModifiedEvent);
    }

    [Fact]
    public void AddNewItem_Should_Reject_Same_Product_With_Different_Price()
    {
        var sale = CreateSale();
        var productId = Guid.NewGuid();

        sale.AddNewItem(
            productId,
            "Mouse",
            2,
            100m,
            _discountPolicy);

        var act = () => sale.AddNewItem(
            productId,
            "Mouse",
            1,
            120m,
            _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage(
                "The same product cannot be added with a different unit price.");
    }

    [Fact]
    public void UpdateItem_Should_Update_Existing_Item_And_Recalculate_Discount()
    {
        var sale = CreateSale();
        var productId = Guid.NewGuid();

        var item = sale.AddNewItem(
            productId,
            "Mouse",
            2,
            100m,
            _discountPolicy);

        sale.ClearEvents();

        sale.UpdateItem(
            item.Id,
            productId,
            " Mouse Pro ",
            10,
            100m,
            _discountPolicy);

        item.ProductName.Should().Be("Mouse Pro");
        item.Quantity.Should().Be(10);
        item.UnitPrice.Should().Be(100m);
        item.DiscountPercentage.Should().Be(20m);
        item.DiscountAmount.Should().Be(200m);
        item.TotalAmount.Should().Be(800m);
        sale.TotalAmount.Should().Be(800m);
    }

    [Fact]
    public void UpdateItem_Should_Reject_Duplicate_Active_Product()
    {
        var sale = CreateSale();
        var firstProduct = Guid.NewGuid();
        var secondProduct = Guid.NewGuid();

        sale.AddNewItem(
            firstProduct,
            "Mouse",
            2,
            100m,
            _discountPolicy);

        var secondItem = sale.AddNewItem(
            secondProduct,
            "Keyboard",
            2,
            200m,
            _discountPolicy);

        var act = () => sale.UpdateItem(
            secondItem.Id,
            firstProduct,
            "Keyboard",
            2,
            200m,
            _discountPolicy);

        act
            .Should()
            .Throw<DomainException>()
            .WithMessage(
                "The same product cannot be added more than once to the sale.");
    }

    [Fact]
    public void UpdateItem_Should_Throw_When_Item_Does_Not_Exist()
    {
        var sale = CreateSale();

        var act = () => sale.UpdateItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Mouse",
            1,
            10m,
            _discountPolicy);

        act
            .Should()
            .Throw<KeyNotFoundException>()
            .WithMessage("Sale item with ID * not found.");
    }

    [Fact]
    public void CancelItem_Should_Mark_Item_Cancelled_And_Remove_It_From_Total()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            4,
            100m,
            _discountPolicy);

        sale.ClearEvents();

        sale.CancelItem(item.Id);

        item.IsCancelled.Should().BeTrue();
        item.TotalAmount.Should().Be(0m);
        sale.TotalAmount.Should().Be(0m);

        sale.Events
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ItemCancelledEvent>();
    }

    [Fact]
    public void CancelItem_Should_Create_ItemCancelled_Event_Each_Time_It_Is_Called()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            2,
            100m,
            _discountPolicy);

        sale.ClearEvents();

        sale.CancelItem(item.Id);
        sale.CancelItem(item.Id);

        item.IsCancelled.Should().BeTrue();
        item.TotalAmount.Should().Be(0m);
        sale.Events.Should().HaveCount(2);
        sale.Events.Should().AllBeOfType<ItemCancelledEvent>();
    }

    [Fact]
    public void Cancel_Should_Set_Status_And_Create_Event()
    {
        var sale = CreateSale();
        sale.ClearEvents();

        sale.Cancel();

        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.UpdatedAt.Should().NotBeNull();

        sale.Events
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<SaleCancelledEvent>();
    }

    [Fact]
    public void Cancel_Should_Be_Idempotent()
    {
        var sale = CreateSale();
        sale.ClearEvents();

        sale.Cancel();
        sale.Cancel();

        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.Events.Should().ContainSingle(x => x is SaleCancelledEvent);
    }

    [Fact]
    public void GetItem_Should_Return_Item_By_Id()
    {
        var sale = CreateSale();

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            1,
            10m,
            _discountPolicy);

        sale.GetItem(item.Id).Should().BeSameAs(item);
    }

    [Fact]
    public void GetItem_Should_Throw_When_Item_Does_Not_Exist()
    {
        var sale = CreateSale();

        var act = () => sale.GetItem(Guid.NewGuid());

        act
            .Should()
            .Throw<KeyNotFoundException>()
            .WithMessage("Sale item with ID * not found.");
    }

    [Fact]
    public void TotalAmount_Should_Ignore_Cancelled_Items()
    {
        var sale = CreateSale();

        var first = sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            2,
            100m,
            _discountPolicy);

        sale.AddNewItem(
            Guid.NewGuid(),
            "Keyboard",
            2,
            200m,
            _discountPolicy);

        sale.CancelItem(first.Id);

        sale.TotalAmount.Should().Be(400m);
    }

    [Fact]
    public void ClearEvents_Should_Remove_All_Domain_Events()
    {
        var sale = CreateSale();

        sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            1,
            10m,
            _discountPolicy);

        sale.Events.Should().NotBeEmpty();

        sale.ClearEvents();

        sale.Events.Should().BeEmpty();
    }

    private static Sale CreateSale() =>
        new(
            "SALE-ENTITY-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());
}