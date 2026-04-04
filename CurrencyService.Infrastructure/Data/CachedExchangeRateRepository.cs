using CurrencyService.Application.Interfaces;
using ZiggyCreatures.Caching.Fusion;

namespace CurrencyService.Infrastructure.Data;

internal sealed class CachedExchangeRateRepository(IFusionCache cache) : IExchangeRateRepository
{
    public async Task<decimal?> GetAsync(string currency, CancellationToken cancellationToken = default) =>
        await cache.GetOrDefaultAsync<decimal?>(
            $"ExchangeRates:{currency}",
            token: cancellationToken);

    public async Task SetAsync(string currency, decimal rate, CancellationToken cancellationToken = default) =>
        await cache.SetAsync(
            $"ExchangeRates:{currency}",
            rate,
            new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) },
            token: cancellationToken);
}
