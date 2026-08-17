using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSale;

public sealed class ListSalesHandlerTests
{
    [Fact(DisplayName = "Should return mapped sales")]
    public async Task Handle_Should_Return_Mapped_Sales()
    {
        var sales = new[]
        {
            new Sale(
                "SALE-001",
                DateTime.UtcNow,
                Guid.NewGuid(),
                Guid.NewGuid()),

            new Sale(
                "SALE-002",
                DateTime.UtcNow,
                Guid.NewGuid(),
                Guid.NewGuid())
        };

        foreach (var sale in sales)
        {
            sale.ClearEvents();
        }

        var repository = Substitute.For<ISaleRepository>();

        repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(sales);

        var result = await CreateHandler(repository)
            .Handle(
                new ListSalesCommand(),
                CancellationToken.None);

        result.Should().HaveCount(2);

        result
            .Select(x => x.SaleNumber)
            .Should()
            .Contain(new[] { "SALE-001", "SALE-002" });
    }

    [Fact(DisplayName = "Should return a empty list when has no sales")]
    public async Task Handle_Should_Return_Empty_When_Repository_Returns_No_Sales()
    {
        var repository = Substitute.For<ISaleRepository>();

        repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());

        var result = await CreateHandler(repository)
            .Handle(
                new ListSalesCommand(),
                CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static ListSalesHandler CreateHandler(
        ISaleRepository repository) =>
        new(
            repository,
            new MapperConfiguration(
                cfg => cfg.AddProfile<SaleProfile>())
                .CreateMapper(),
            Substitute.For<ILogger<ListSalesHandler>>());
}