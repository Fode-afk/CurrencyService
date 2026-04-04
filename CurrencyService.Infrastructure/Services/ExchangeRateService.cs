using CurrencyService.Application.Interfaces;
using migApp.Shared.Domain.ValueObjects;

namespace CurrencyService.Infrastructure.Services;

internal sealed class ExchangeRateService(
    IExchangeRateProvider provider,
    IExchangeRateRepository repo) : IExchangeRateService
{
    public async Task<decimal?> GetExchangeRateAsync(
        Currency from,
        Currency to,
        CancellationToken ct = default)
    {
        if (from == to)
            return 1m;

        var fromRate = await GetOrFetch(from.Code, ct);
        var toRate = await GetOrFetch(to.Code, ct);

        if (fromRate is null || toRate is null)
            return null;

        return toRate.Value / fromRate.Value;
    }

    private async Task<decimal?> GetOrFetch(string currency, CancellationToken ct)
    {
        var cached = await repo.GetAsync(currency, ct);
        if (cached is not null)
            return cached;

        var rate = await provider.GetRateAsync(currency, ct);
        if (rate is null)
            return null;

        await repo.SetAsync(currency, rate.Value, ct);

        return rate;
    }

    public async Task UpdateExchangeRatesAsync(CancellationToken ct = default)
    {
        var supported = new[] { "USD", "EUR", "BYN", "RUB" };

        var rates = await provider.GetRatesAsync(supported, ct);

        foreach (var (currency, rate) in rates)
            await repo.SetAsync(currency, rate, ct);
    }
}