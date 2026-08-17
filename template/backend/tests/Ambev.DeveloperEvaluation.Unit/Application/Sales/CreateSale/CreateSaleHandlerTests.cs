using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CreateSale;

public sealed class CreateSaleHandlerTests
{
    [Fact(DisplayName = "Should create a sale and publish a event")]
    public async Task Handle_Should_Create_Sale_Persist_And_Publish_Events()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<ISaleEventPublisher>();
        var handler = CreateHandler(repository, publisher);
        var command = ValidCommand();

        repository
            .CreateIdempotentAsync(
                Arg.Any<Sale>(),
                command.IdempotencyKey,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(call => (call.Arg<Sale>(), false));

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        result.IsIdempotentReplay.Should().BeFalse();
        result.Sale.SaleNumber.Should().Be(command.SaleNumber);
        result.Sale.TotalAmount.Should().Be(800m);

        await repository
            .Received(1)
            .CreateIdempotentAsync(
                Arg.Any<Sale>(),
                command.IdempotencyKey,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await publisher
            .Received(1)
            .PublishAsync(
                Arg.Any<SaleCreatedEvent>(),
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should return a idempotent replay and not publish a event")]
    public async Task Handle_Should_Return_Replay_And_Not_Publish_Event()
    {
        var command = ValidCommand();

        var existing = new Sale(
            command.SaleNumber,
            command.Date,
            command.CustomerId,
            command.BranchId);

        existing.ClearEvents();

        var repository = Substitute.For<ISaleRepository>();

        repository
            .CreateIdempotentAsync(
                Arg.Any<Sale>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((existing, true));

        var publisher = Substitute.For<ISaleEventPublisher>();
        var handler = CreateHandler(repository, publisher);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        result.IsIdempotentReplay.Should().BeTrue();
        result.Sale.Id.Should().Be(existing.Id);

        await publisher
            .DidNotReceive()
            .PublishAsync(
                Arg.Any<ISaleEvent>(),
                Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Should throw a exception when has sale number duplicate")]
    public async Task Handle_Should_Propagate_Duplicate_Sale_Number_Error()
    {
        var command = ValidCommand();
        var repository = Substitute.For<ISaleRepository>();

        repository
            .CreateIdempotentAsync(
                Arg.Any<Sale>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<(Sale Sale, bool IsReplay)>(
                    new InvalidOperationException(
                        "Sale number 'SALE-001' already exists.")));

        var handler = CreateHandler(
            repository,
            Substitute.For<ISaleEventPublisher>());

        var act = () => handler.Handle(
            command,
            CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Sale number 'SALE-001' already exists.");
    }

    [Fact(DisplayName = "Validator should reject duplicate products and invalid fields")]
    public async Task Validator_Should_Reject_Duplicate_Products_And_Invalid_Fields()
    {
        var command = ValidCommand();

        command.IdempotencyKey = "";
        command.Items[0].ProductId = Guid.Empty;
        command.Items.Add(command.Items[0]);

        var result = await new CreateSaleValidator()
            .ValidateAsync(command);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "IdempotencyKey");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Items[0].ProductId");

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage.Contains(
                    "same product",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Validator should reject quantity above 20 and non positive price")]
    public async Task Validator_Should_Reject_Quantity_Above_20_And_NonPositive_Price()
    {
        var command = ValidCommand();

        command.Items[0].Quantity = 21;
        command.Items[0].UnitPrice = 0;

        var result = await new CreateSaleValidator()
            .ValidateAsync(command);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Items[0].Quantity");

        result.Errors
            .Should()
            .Contain(x => x.PropertyName == "Items[0].UnitPrice");
    }

    private static CreateSaleCommand ValidCommand() =>
        new()
        {
            IdempotencyKey = "idem-001",
            SaleNumber = "SALE-001",
            Date = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            Items =
            [
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product",
                    Quantity = 10,
                    UnitPrice = 100m
                }
            ]
        };

    private static CreateSaleHandler CreateHandler(
        ISaleRepository repository,
        ISaleEventPublisher publisher) =>
        new(
            repository,
            new ProgressiveDiscountPolicy(),
            new MapperConfiguration(
                cfg => cfg.AddProfile<SaleProfile>())
                .CreateMapper(),
            publisher,
            Substitute.For<ILogger<CreateSaleHandler>>());
}