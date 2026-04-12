namespace CurrencyService.Application.Caching;

public static class CacheKeys
{
    public static string ExchangeRateByCurrency(string currency) => $"ExchangeRate:{currency}";
}