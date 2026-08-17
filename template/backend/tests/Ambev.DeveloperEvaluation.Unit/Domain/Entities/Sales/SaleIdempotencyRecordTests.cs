using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.Sales;

public sealed class SaleIdempotencyRecordTests
{
    [Fact(DisplayName = "Should create a record with expected values")]
    public void Constructor_Should_Create_Record_With_Expected_Values()
    {
        var saleId = Guid.NewGuid();

        var record = new SaleIdempotencyRecord(
            "  idem-001  ",
            "hash-001",
            saleId);

        record.Id.Should().NotBe(Guid.Empty);
        record.Key.Should().Be("idem-001");
        record.RequestHash.Should().Be("hash-001");
        record.SaleId.Should().Be(saleId);
        record.CreatedAt.Should().BeCloseTo(
            DateTime.UtcNow,
            TimeSpan.FromSeconds(2));
    }

    [Theory(DisplayName = "Should reject a invalid idempotency key")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Reject_Invalid_Idempotency_Key(
        string? key)
    {
        var act = () => new SaleIdempotencyRecord(
            key!,
            "hash-001",
            Guid.NewGuid());

        act
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("key")
            .WithMessage(
                "Idempotency key is required. (Parameter 'key')");
    }

    [Theory(DisplayName = "Should reject a invalid request hash")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Reject_Invalid_Request_Hash(
        string? requestHash)
    {
        var act = () => new SaleIdempotencyRecord(
            "idem-001",
            requestHash!,
            Guid.NewGuid());

        act
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("requestHash")
            .WithMessage(
                "Request hash is required. (Parameter 'requestHash')");
    }

    [Fact(DisplayName = "Should allow a empty sale id because entity does not validate it")]
    public void Constructor_Should_Allow_Empty_Sale_Id_Because_Entity_Does_Not_Validate_It()
    {
        var record = new SaleIdempotencyRecord(
            "idem-001",
            "hash-001",
            Guid.Empty);

        record.SaleId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "Should create a unique id for each record")]
    public void Constructor_Should_Create_A_Unique_Id_For_Each_Record()
    {
        var first = new SaleIdempotencyRecord(
            "idem-001",
            "hash-001",
            Guid.NewGuid());

        var second = new SaleIdempotencyRecord(
            "idem-002",
            "hash-002",
            Guid.NewGuid());

        first.Id.Should().NotBe(second.Id);
    }
}