using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Adapters.Driven.Infrastructure.Repositories;

[Collection(PostgreSqlCollection.Name)]
public sealed class SalesRepositoryIntegrationTests
{
    private readonly PostgreSqlContainerFixture _fixture;
    private readonly IDiscountPolicy _discountPolicy = new ProgressiveDiscountPolicy();

    public SalesRepositoryIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Should persist sale with itens and calculated totals")]
    public async Task CreateAsync_Should_Persist_Sale_With_Items_And_Calculated_Totals()
    {
        await using var context = _fixture.CreateContext();
        var repository = new SaleRepository(context);

        var sale = CreateSale("INT-CREATE-001");
        sale.AddNewItem(Guid.NewGuid(), "Mouse", 4, 100m, _discountPolicy);

        await repository.CreateAsync(sale);

        await using var verificationContext = _fixture.CreateContext();
        var persisted = await new SaleRepository(verificationContext).GetByIdAsync(sale.Id);

        persisted.Should().NotBeNull();
        persisted!.SaleNumber.Should().Be("INT-CREATE-001");
        persisted.Items.Should().ContainSingle();
        persisted.Items.Single().Quantity.Should().Be(4);
        persisted.Items.Single().DiscountPercentage.Should().Be(10m);
        persisted.Items.Single().DiscountAmount.Should().Be(40m);
        persisted.Items.Single().TotalAmount.Should().Be(360m);
        persisted.TotalAmount.Should().Be(360m);
    }

    [Fact(DisplayName = "Should consolidate existing product through persisted aggregate")]
    public async Task AddItem_Through_Persisted_Aggregate_Should_Consolidate_Existing_Product()
    {
        var productId = Guid.NewGuid();
        var sale = CreateSale("INT-ADD-001");
        sale.AddNewItem(productId, "Mouse", 3, 100m, _discountPolicy);

        await using (var context = _fixture.CreateContext())
        {
            await new SaleRepository(context).CreateAsync(sale);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new SaleRepository(context);
            var persisted = await repository.GetByIdAsync(sale.Id);
            persisted.Should().NotBeNull();

            persisted!.AddNewItem(productId, "Mouse", 2, 100m, _discountPolicy);
            await repository.UpdateAsync(persisted);
        }

        await using var verificationContext = _fixture.CreateContext();
        var result = await new SaleRepository(verificationContext).GetByIdAsync(sale.Id);

        result!.Items.Should().ContainSingle();
        result.Items.Single().Quantity.Should().Be(5);
        result.Items.Single().DiscountPercentage.Should().Be(10m);
        result.Items.Single().TotalAmount.Should().Be(450m);
    }

    [Fact(DisplayName = "Should return the same sale on idempotency replay")]
    public async Task CreateIdempotentAsync_Should_Return_The_Same_Sale_On_Replay()
    {
        const string key = "INT-IDEM-001";
        const string hash = "hash-001";
        var sale = CreateSale("INT-IDEM-001");
        sale.AddNewItem(Guid.NewGuid(), "Keyboard", 10, 250m, _discountPolicy);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new SaleRepository(context);
            var first = await repository.CreateIdempotentAsync(sale, key, hash);

            first.IsReplay.Should().BeFalse();
            first.Sale.Id.Should().Be(sale.Id);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new SaleRepository(context);
            var second = await repository.CreateIdempotentAsync(
                CreateSale("INT-IDEM-001-DUMMY"),
                key,
                hash);

            second.IsReplay.Should().BeTrue();
            second.Sale.Id.Should().Be(sale.Id);
        }

        await using var verificationContext = _fixture.CreateContext();
        var saleCount = await verificationContext.Sales.CountAsync(x => x.Id == sale.Id);
        var idempotencyCount = await verificationContext.SaleIdempotencyRecords.CountAsync(x => x.Key == key);

        saleCount.Should().Be(1);
        idempotencyCount.Should().Be(1);
    }

    [Fact(DisplayName = "Should reject the same idempotency key with diferent request")]
    public async Task CreateIdempotentAsync_Should_Reject_Same_Key_With_Different_Request()
    {
        const string key = "INT-IDEM-CONFLICT-001";

        await using var context = _fixture.CreateContext();
        var repository = new SaleRepository(context);
        var sale = CreateSale("INT-IDEM-CONFLICT-001");

        await repository.CreateIdempotentAsync(sale, key, "hash-original");

        var act = () => repository.CreateIdempotentAsync(
            CreateSale("INT-IDEM-CONFLICT-001-SECOND"),
            key,
            "hash-different");

        await act.Should().ThrowAsync<IdempotencyConflictException>()
            .WithMessage("The Idempotency-Key was already used with a different request.");
    }

    [Fact(DisplayName = "Should reject a duplicate sale number")]
    public async Task CreateIdempotentAsync_Should_Reject_Duplicate_SaleNumber()
    {
        const string saleNumber = "INT-DUPLICATE-001";

        await using var context = _fixture.CreateContext();
        var repository = new SaleRepository(context);

        await repository.CreateAsync(CreateSale(saleNumber));

        var act = () => repository.CreateIdempotentAsync(
            CreateSale(saleNumber),
            "INT-DUPLICATE-KEY-001",
            "hash-duplicate");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Sale number '{saleNumber}' already exists.*");
    }

    private static Sale CreateSale(string saleNumber)
    {
        return new Sale(
            saleNumber,
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid());
    }
}
