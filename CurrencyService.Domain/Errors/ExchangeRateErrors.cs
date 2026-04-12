using migApp.Shared.Results;

namespace CurrencyService.Domain.Errors;

public static class ExchangeRateErrors
{
    public static Error InvalidRate() => Error.InvalidArgument(ExchangeRateErrorCodes.InvalidRate);
    public static Error NotFound() => Error.NotFound(ExchangeRateErrorCodes.NotFound);
}

public static class ExchangeRateErrorCodes
{
    public const string InvalidRate = "ExchangeRate.InvalidRate";
    public const string NotFound = "ExchangeRate.NotFound";
}