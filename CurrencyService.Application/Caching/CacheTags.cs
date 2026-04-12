using migApp.Shared.Domain.ValueObjects;

namespace CurrencyService.Application.Caching;

public static class CacheTags
{
    public static string ExchangeRateByBaseCurrency() => $"ExchangeRateByBaseCurrency:{Currency.USD.Code}";
}