using CurrencyService.Domain.Errors;
using migApp.Shared.Domain.ValueObjects;
using migApp.Shared.Results;
using static migApp.Shared.Results.ResultFactory;

namespace CurrencyService.Domain.Models;

public sealed class ExchangeRate
{
    private ExchangeRate(
        Currency currency,
        decimal rate,
        DateTimeOffset updatedAt)
    {
        Currency = currency;
        Rate = rate;
        UpdatedAt = updatedAt;
    }

    public static Currency BaseCurrency => Currency.USD;
    public Currency Currency { get; private set; }
    public decimal Rate { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static IResult<ExchangeRate> Create(
        Currency currency,
        decimal rate,
        DateTimeOffset now)
    {
        if (rate <= 0)
            return Fail<ExchangeRate>(ExchangeRateErrors.InvalidRate());

        var exchangeRate = new ExchangeRate(
            currency,
            rate,
            now);

        return Ok(exchangeRate);
    }

    public IResult Update(decimal rate, DateTimeOffset now)
    {
        if (rate <= 0)
            return Fail<ExchangeRate>(ExchangeRateErrors.InvalidRate());

        Rate = rate;
        UpdatedAt = now;

        return Ok();
    }
}
