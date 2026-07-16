using CurrencyService.Application.Caching;
using CurrencyService.Application.Interfaces.Data;
using CurrencyService.Application.Interfaces.Metrics;
using CurrencyService.Domain.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using migApp.Shared.Domain.ValueObjects;
using migApp.Shared.Results;
using ZiggyCreatures.Caching.Fusion;
using static migApp.Shared.Results.ResultFactory;

namespace CurrencyService.Application.Features.Queries.GetExchangeRate;

public sealed class GetExchangeRateQueryHandler(
    IAppDbContext context,
    ICurrencyMetrics metrics,
    IFusionCache cache) : IRequestHandler<GetExchangeRateQuery, IResult<decimal>>
{
    public async Task<IResult<decimal>> Handle(GetExchangeRateQuery request, CancellationToken cancellationToken)
    {
        var currencyFromResult = Currency.Create(request.CurrencyFrom);
        if (currencyFromResult.IsFailure)
            return Fail<decimal>(currencyFromResult.Error);

        var currencyToResult = Currency.Create(request.CurrencyTo);
        if (currencyToResult.IsFailure)
            return Fail<decimal>(currencyToResult.Error);

        var from = currencyFromResult.Value;
        var to = currencyToResult.Value;

        if (from == to)
            return Ok(1m);

        var wasHit = true;

        var fromRate = await cache.GetOrSetAsync<decimal?>(
            CacheKeys.ExchangeRateByCurrency(from.Code),
            async (entry, ct) =>
            {
                wasHit = false;

                return await context.ExchangeRates
                    .Where(r => r.Currency == from)
                    .Select(r => r.Rate)
                    .FirstOrDefaultAsync(ct);
            },
            tags: [CacheTags.ExchangeRateByBaseCurrency()],
            token: cancellationToken);

        metrics.RecordCacheHitOrMiss("exchange-rate-by-base-currency", wasHit);

        if (fromRate == null)
            return Fail<decimal>(ExchangeRateErrors.NotFound());

        wasHit = true;

        var toRate = await cache.GetOrSetAsync<decimal?>(
            CacheKeys.ExchangeRateByCurrency(to.Code),
            async (entry, ct) =>
            {
                wasHit = false;

                return await context.ExchangeRates
                    .Where(r => r.Currency == to)
                    .Select(r => r.Rate)
                    .FirstOrDefaultAsync(ct);
            },
            tags: [CacheTags.ExchangeRateByBaseCurrency()],
            token: cancellationToken);

        metrics.RecordCacheHitOrMiss("exchange-rate-by-base-currency", wasHit);

        if (toRate == null)
            return Fail<decimal>(ExchangeRateErrors.NotFound());

        return Ok(toRate.Value / fromRate.Value);
    }
}
