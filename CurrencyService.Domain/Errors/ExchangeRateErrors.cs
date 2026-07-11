using migApp.Shared.Results;

namespace CurrencyService.Domain.Errors;

public static class ExchangeRateErrors
{
    public static Error InvalidRate() =>
        Error.InvalidArgument(ExchangeRateErrorCodes.InvalidRate,
            "The provided exchange rate is invalid.");

    public static Error NotFound() =>
        Error.NotFound(ExchangeRateErrorCodes.NotFound,
            "The requested exchange rate was not found.");
}

public static class ExchangeRateErrorCodes
{
    public const string InvalidRate = "ExchangeRate.InvalidRate";
    public const string NotFound = "ExchangeRate.NotFound";
}