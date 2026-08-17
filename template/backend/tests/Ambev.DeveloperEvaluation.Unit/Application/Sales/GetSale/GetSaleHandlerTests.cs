using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.GetSale;

public sealed class GetSaleHandlerTests
{
    [Fact(DisplayName = "Should return mapped sale")]
    public async Task Handle_Should_Return_Mapped_Sale()
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

        var result = await CreateHandler(repository)
            .Handle(
                new GetSaleCommand
                {
                    Id = sale.Id
                },
                CancellationToken.None);

        result.Id.Should().Be(sale.Id);
        result.SaleNumber.Should().Be("SALE-001");
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

        var act = () => CreateHandler(repository)
            .Handle(
                new GetSaleCommand
                {
                    Id = id
                },
                CancellationToken.None);

        await act
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sale with ID {id} not found.");
    }

    [Fact(DisplayName = "Validator should required id")]
    public async Task Validator_Should_Require_Id()
    {
        var result = await new GetSaleValidator()
            .ValidateAsync(new GetSaleCommand());

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .ContainSingle(x => x.PropertyName == "Id");
    }

    private static GetSaleHandler CreateHandler(
        ISaleRepository repository) =>
        new(
            repository,
            new MapperConfiguration(
                cfg => cfg.AddProfile<SaleProfile>())
                .CreateMapper(),
            Substitute.For<ILogger<GetSaleHandler>>());
}