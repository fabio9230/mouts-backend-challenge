using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public sealed class GetSaleValidator : AbstractValidator<GetSaleCommand>
{
    public GetSaleValidator() => RuleFor(x => x.Id).NotEmpty();
}
