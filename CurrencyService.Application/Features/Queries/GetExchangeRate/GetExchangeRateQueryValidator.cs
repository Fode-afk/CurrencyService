using FluentValidation;
using migApp.Shared.Domain.Errors;

namespace CurrencyService.Application.Features.Queries.GetExchangeRate;

public sealed class GetExchangeRateQueryValidator : AbstractValidator<GetExchangeRateQuery>
{
    public GetExchangeRateQueryValidator()
    {
        RuleFor(x => x.CurrencyFrom)
            .NotEmpty()
            .Length(3)
            .WithErrorCode(CurrencyErrorCodes.InvalidFormat);

        RuleFor(x => x.CurrencyTo)
            .NotEmpty()
            .Length(3)
            .WithErrorCode(CurrencyErrorCodes.InvalidFormat);
    }
}
