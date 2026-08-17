using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelItem;

public sealed class CancelItemValidator : AbstractValidator<CancelItemCommand>
{
    public CancelItemValidator()
    {
        RuleFor(x => x.SaleId)
            .NotEmpty();

        RuleFor(x => x.ItemId)
            .NotEmpty();
    }
}
