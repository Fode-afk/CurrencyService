using FluentValidation;
using migApp.Shared.Domain.Errors;

namespace CurrencyService.Application.Features.Commands.Convert;

public sealed class ConvertCommandValidator : AbstractValidator<ConvertCommand>
{
    public ConvertCommandValidator()
    {
        RuleFor(x => x.SourceMoneyMinor)
           .GreaterThanOrEqualTo(0)
           .WithErrorCode(MoneyErrorCodes.AmountNegative);

        RuleFor(x => x.SourceCurrency)
            .NotEmpty()
            .Length(3)
            .WithErrorCode(CurrencyErrorCodes.InvalidFormat);

        RuleFor(x => x.TargetCurrency)
            .NotEmpty()
            .Length(3)
            .WithErrorCode(CurrencyErrorCodes.InvalidFormat);
    }
}
