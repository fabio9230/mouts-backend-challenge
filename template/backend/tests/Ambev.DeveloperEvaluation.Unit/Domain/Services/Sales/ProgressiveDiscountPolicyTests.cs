using Ambev.DeveloperEvaluation.Domain.Services.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services.Sales;

public sealed class ProgressiveDiscountPolicyTests
{
    private readonly ProgressiveDiscountPolicy _sut = new();

    [Theory(DisplayName = "Should calculate progressive discount")]
    [InlineData(1, 100, 0)]
    [InlineData(3, 100, 0)]
    [InlineData(4, 100, 40)]
    [InlineData(9, 100, 90)]
    [InlineData(10, 100, 200)]
    [InlineData(20, 100, 400)]
    public void Should_Calculate_Progressive_Discount(int quantity, decimal unitPrice, decimal expected)
    {
        var result = _sut.CalculateDiscountAmount(quantity, unitPrice);

        result.Should().Be(expected);
    }

    [Theory(DisplayName = "Should return expected discount percentage")]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 20)]
    [InlineData(20, 20)]
    public void Should_Return_Expected_Discount_Percentage(int quantity, decimal expected)
    {
        _sut.CalculateDiscountPercentage(quantity).Should().Be(expected / 100m);
    }
}