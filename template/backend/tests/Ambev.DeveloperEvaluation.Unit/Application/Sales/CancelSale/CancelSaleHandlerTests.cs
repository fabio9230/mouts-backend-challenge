using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelSale;

public sealed class CancelSaleHandlerTests
{
    [Fact(DisplayName = "Should cancel a sale update and publish a event")]
    public async Task Handle_Should_Cancel_Sale_Update_Repository_And_Publish_Event()
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

        var publisher = Substitute.For<ISaleEventPublisher>();
        var handler = CreateHandler(repository, publisher);

        var result = await handler.Handle(
            new CancelSaleCommand
            {
                Id = sale.Id
            },
            CancellationToken.None);

        result.Status.Should().Be("Cancelled");

        await repository
            .Received(1)
            .UpdateAsync(
                sale,
                Arg.Any<CancellationToken>());

        await publisher
            .Received(1)
            .PublishAsync(
                Arg.Any<SaleCancelledEvent>(),
                Arg.Any<CancellationToken>());

        sale.Events.Should().BeEmpty();
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

        var handler = CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>());

        var act = () => handler.Handle(
            new CancelSaleCommand
            {
                Id = id
            },
            CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {id} not found.");
    }

    [Fact(DisplayName = "Should be idempotent when sale is already cancel and not publish a event")]
    public async Task Handle_Should_Be_Idempotent_When_Sale_Is_Already_Cancelled()
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());

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

        var result = await handler.Handle(
            new CancelSaleCommand
            {
                Id = sale.Id
            },
            CancellationToken.None);

        result.Status.Should().Be("Cancelled");

        await repository
            .Received(1)
            .UpdateAsync(
                sale,
                Arg.Any<CancellationToken>());

        await publisher
            .DidNotReceive()
            .PublishAsync(
                Arg.Any<ISaleEvent>(),
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Validator should required id")]
    public async Task Validator_Should_Require_Id()
    {
        var result = await new CancelSaleValidator()
            .ValidateAsync(new CancelSaleCommand());

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .ContainSingle(x => x.PropertyName == "Id");
    }

    private static CancelSaleHandler CreateHandler(
        ISaleRepository repository,
        ISaleEventPublisher publisher) =>
        new(
            repository,
            CreateMapper(),
            publisher,
            Substitute.For<ILogger<CancelSaleHandler>>());

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            cfg => cfg.AddProfile<SaleProfile>())
        .CreateMapper();
}