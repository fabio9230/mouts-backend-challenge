namespace Ambev.DeveloperEvaluation.Domain.Services.Sales;

public interface IDiscountPolicy
{
    decimal CalculateDiscountAmount(int quantity, decimal unitPrice);
    decimal CalculateDiscountPercentage(int quantity);
}
