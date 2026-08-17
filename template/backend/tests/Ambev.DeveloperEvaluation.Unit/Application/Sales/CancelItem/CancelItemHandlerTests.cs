using Ambev.DeveloperEvaluation.Application.Sales.CancelItem;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelItem;

public sealed class CancelItemHandlerTests
{
    [Fact(DisplayName = "Should cancel a item update and publish a event")]
    public async Task Handle_Should_Cancel_Item_Update_Repository_And_Publish_Event()
    {
        var sale = CreateSaleWithItem(out var item);
        sale.ClearEvents();

        var repository = Substitute.For<ISaleRepository>();
        repository
            .GetByIdAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(sale);

        var publisher = Substitute.For<ISaleEventPublisher>();
        var handler = CreateHandler(repository, publisher);

        var result = await handler.Handle(
            new CancelItemCommand
            {
                SaleId = sale.Id,
                ItemId = item.Id
            },
            CancellationToken.None);

        item.IsCancelled.Should().BeTrue();
        result.TotalAmount.Should().Be(0);

        await repository
            .Received(1)
            .UpdateAsync(
                sale,
                Arg.Any<CancellationToken>());

        await publisher
            .Received(1)
            .PublishAsync(
                Arg.Any<ItemCancelledEvent>(),
                Arg.Any<CancellationToken>());

        sale.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Should throw a exception when sale does not exist")]
    public async Task Handle_Should_Throw_When_Sale_Does_Not_Exist()
    {
        var repository = Substitute.For<ISaleRepository>();

        repository
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns((Sale?)null);

        var handler = CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>());

        var id = Guid.NewGuid();

        var act = () => handler.Handle(
            new CancelItemCommand
            {
                SaleId = id,
                ItemId = Guid.NewGuid()
            },
            CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {id} not found.");
    }

    [Fact(DisplayName = "Should reject a modification item when sale status is cancelled")]
    public async Task Handle_Should_Reject_Item_Modification_When_Sale_Is_Cancelled()
    {
        var sale = CreateSaleWithItem(out var item);

        sale.ClearEvents();
        sale.Cancel();
        sale.ClearEvents();

        var repository = Substitute.For<ISaleRepository>();

        repository
            .GetByIdAsync(
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(sale);

        var publisher = Substitute.For<ISaleEventPublisher>();
        var handler = CreateHandler(repository, publisher);

        var act = () => handler.Handle(
            new CancelItemCommand
            {
                SaleId = sale.Id,
                ItemId = item.Id
            },
            CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A cancelled sale cannot have its items modified.");

        await repository
            .DidNotReceive()
            .UpdateAsync(
                Arg.Any<Sale>(),
                Arg.Any<CancellationToken>());

        await publisher
            .DidNotReceive()
            .PublishAsync(
                Arg.Any<ISaleEvent>(),
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should throw exception when item does not exist")]
    public async Task Handle_Should_Propagate_When_Item_Does_Not_Exist()
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        sale.ClearEvents();

        var repository = Substitute.For<ISaleRepository>();

        repository
            .GetByIdAsync(
                sale.Id,
                Arg.Any<CancellationToken>())
            .Returns(sale);

        var handler = CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>());

        var act = () => handler.Handle(
            new CancelItemCommand
            {
                SaleId = sale.Id,
                ItemId = Guid.NewGuid()
            },
            CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Validator should require saledId and itemId")]
    public async Task Validator_Should_Require_SaleId_And_ItemId()
    {
        var result = await new CancelItemValidator()
            .ValidateAsync(new CancelItemCommand());

        result.IsValid.Should().BeFalse();

        result.Errors
            .Select(x => x.PropertyName)
            .Should()
            .Contain(new[] { "SaleId", "ItemId" });
    }

    private static Sale CreateSaleWithItem(out SaleItem item)
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

        item = sale.AddNewItem(
            Guid.NewGuid(),
            "Product",
            2,
            10m,
            new ProgressiveDiscountPolicy());

        return sale;
    }

    private static CancelItemHandler CreateHandler(
        ISaleRepository repository,
        ISaleEventPublisher publisher) =>
        new(
            repository,
            CreateMapper(),
            publisher,
            Substitute.For<ILogger<CancelItemHandler>>());

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            cfg => cfg.AddProfile<SaleProfile>())
        .CreateMapper();
}