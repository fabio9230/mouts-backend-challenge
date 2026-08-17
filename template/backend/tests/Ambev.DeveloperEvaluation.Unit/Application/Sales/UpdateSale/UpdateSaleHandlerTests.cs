using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.UpdateSale;

public sealed class UpdateSaleHandlerTests
{
    [Fact(DisplayName = "Should update header and existing item")]
    public async Task Handle_Should_Update_Header_And_Existing_Item()
    {
        var policy = new ProgressiveDiscountPolicy();

        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        var item = sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            2,
            100m,
            policy);

        sale.ClearEvents();

        var repository = CreateRepository(sale);

        repository
            .SaleNumberExistsAsync(
                "SALE-002",
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var publisher = Substitute.For<ISaleEventPublisher>();
        var handler = CreateHandler(repository, publisher);

        var command = new UpdateSaleCommand
        {
            Id = sale.Id,
            SaleNumber = "SALE-002",
            Date = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            Items =
            [
                new()
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = "Mouse",
                    Quantity = 5,
                    UnitPrice = 100m
                }
            ]
        };

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        result.SaleNumber.Should().Be("SALE-002");
        result.TotalAmount.Should().Be(450m);
        item.Quantity.Should().Be(5);

        await repository
            .Received(1)
            .UpdateAsync(
                sale,
                Arg.Any<CancellationToken>());

        await publisher
            .Received(1)
            .PublishAsync(
                Arg.Is<SaleModifiedEvent>(e =>
                    e.SaleId == sale.Id &&
                    e.SaleNumber == "SALE-002"),
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should add new item when has saleItemId null")]
    public async Task Handle_Should_Add_New_Item_When_Id_Is_Null()
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            2,
            100m,
            new ProgressiveDiscountPolicy());

        sale.ClearEvents();

        var repository = CreateRepository(sale);

        repository
            .SaleNumberExistsAsync(
                Arg.Any<string>(),
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>());

        var command = BaseCommand(sale);

        command.Items =
        [
            new()
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Keyboard",
                Quantity = 2,
                UnitPrice = 200m
            }
        ];

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        sale.Items
            .Should()
            .ContainSingle(x => x.ProductName == "Keyboard");

        result.Items
            .Should()
            .Contain(x => x.ProductName == "Keyboard");

        await repository
            .Received(1)
            .UpdateAsync(
                sale,
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should update a existing saleItem and add a new one")]
    public async Task Handle_Should_Update_Existing_And_Add_New_Item()
    {
        var policy = new ProgressiveDiscountPolicy();

        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        var existing = sale.AddNewItem(
            Guid.NewGuid(),
            "Mouse",
            2,
            100m,
            policy);

        sale.ClearEvents();

        var repository = CreateRepository(sale);

        repository
            .SaleNumberExistsAsync(
                Arg.Any<string>(),
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>());

        var command = BaseCommand(sale);

        command.Items =
        [
            new()
            {
                Id = existing.Id,
                ProductId = existing.ProductId,
                ProductName = "Mouse",
                Quantity = 5,
                UnitPrice = 100m
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Keyboard",
                Quantity = 2,
                UnitPrice = 200m
            }
        ];

        await handler.Handle(
            command,
            CancellationToken.None);

        existing.Quantity.Should().Be(5);

        sale.Items
            .Should()
            .ContainSingle(x => x.ProductName == "Keyboard");

        await repository
            .Received(1)
            .UpdateAsync(
                sale,
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should reject a duplicate sale number")]
    public async Task Handle_Should_Reject_Duplicate_Sale_Number()
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        sale.ClearEvents();

        var repository = CreateRepository(sale);

        repository
            .SaleNumberExistsAsync(
                "SALE-002",
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>());

        var command = BaseCommand(sale);
        command.SaleNumber = "SALE-002";

        var act = () => handler.Handle(
            command,
            CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Sale number 'SALE-002' already exists.");

        await repository
            .DidNotReceive()
            .UpdateAsync(
                Arg.Any<Sale>(),
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should reject a cancelled sale")]
    public async Task Handle_Should_Reject_Cancelled_Sale()
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        sale.ClearEvents();
        sale.Cancel();
        sale.ClearEvents();

        var repository = CreateRepository(sale);

        repository
            .SaleNumberExistsAsync(
                Arg.Any<string>(),
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var act = () => CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>())
            .Handle(
                BaseCommand(sale),
                CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A cancelled sale cannot be modified.");
    }

    [Fact(DisplayName = "Should throw a exception when sale does not exist")]
    public async Task Handle_Should_Throw_When_Sale_Does_Not_Exist()
    {
        var id = Guid.NewGuid();

        var repository = Substitute.For<ISaleRepository>();

        repository
            .GetByIdAsync(
                id,
                Arg.Any<CancellationToken>())
            .Returns((Sale?)null);

        var command = new UpdateSaleCommand
        {
            Id = id,
            SaleNumber = "SALE-001",
            Date = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            Items =
            [
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "P",
                    Quantity = 1,
                    UnitPrice = 10m
                }
            ]
        };

        var act = () => CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>())
            .Handle(
                command,
                CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {id} not found.");
    }

    [Fact(DisplayName = "Validator should reject invalid header and item")]
    public async Task Validator_Should_Reject_Invalid_Header_And_Item()
    {
        var command = new UpdateSaleCommand
        {
            Items =
            [
                new()
                {
                    Quantity = 21,
                    UnitPrice = 0,
                    ProductName = "",
                    ProductId = Guid.Empty
                }
            ]
        };

        var result = await new UpdateSaleValidator()
            .ValidateAsync(command);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Id");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "SaleNumber");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "CustomerId");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "BranchId");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Items[0].ProductId");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Items[0].Quantity");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Items[0].UnitPrice");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Items[0].ProductName");
    }

    [Fact(DisplayName = "Validator should reject duplicate products")]
    public async Task Validator_Should_Reject_Duplicate_Products()
    {
        var product = Guid.NewGuid();

        var command = BaseCommand(
            new Sale(
                "SALE-001",
                DateTime.UtcNow,
                Guid.NewGuid(),
                Guid.NewGuid()));

        command.Items =
        [
            new()
            {
                ProductId = product,
                ProductName = "P1",
                Quantity = 1,
                UnitPrice = 10m
            },
            new()
            {
                ProductId = product,
                ProductName = "P2",
                Quantity = 1,
                UnitPrice = 20m
            }
        ];

        var result = await new UpdateSaleValidator()
            .ValidateAsync(command);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage.Contains(
                    "same product",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static ISaleRepository CreateRepository(Sale sale)
    {
        var repository = Substitute.For<ISaleRepository>();

        repository
            .GetByIdAsync(
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(sale);

        return repository;
    }

    private static UpdateSaleCommand BaseCommand(Sale sale) =>
        new()
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            Date = sale.Date,
            CustomerId = sale.CustomerId,
            BranchId = sale.BranchId,
            Items = []
        };

    private static UpdateSaleHandler CreateHandler(
        ISaleRepository repository,
        ISaleEventPublisher publisher) =>
        new(
            repository,
            new ProgressiveDiscountPolicy(),
            new MapperConfiguration(
                cfg => cfg.AddProfile<SaleProfile>())
                .CreateMapper(),
            publisher,
            Substitute.For<ILogger<UpdateSaleHandler>>());
}