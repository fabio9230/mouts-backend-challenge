namespace Ambev.DeveloperEvaluation.Domain.Services.Sales;

public sealed class ProgressiveDiscountPolicy : IDiscountPolicy
{
    public decimal CalculateDiscountAmount(
        int quantity,
        decimal unitPrice)
    {
        var percentage = CalculateDiscountPercentage(quantity);

        return decimal.Round(
            quantity * unitPrice * percentage,
            2,
            MidpointRounding.AwayFromZero);
    }

    public decimal CalculateDiscountPercentage(int quantity)
    {
        return quantity switch
        {
            >= 10 and <= 20 => 0.20m,
            >= 4 and < 10 => 0.10m,
            _ => 0m
        };
    }
}
