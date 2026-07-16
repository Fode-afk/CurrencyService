using CurrencyService.Application.Caching;
using CurrencyService.Application.Interfaces.Data;
using CurrencyService.Application.Interfaces.Metrics;
using CurrencyService.Application.Interfaces.Services;
using CurrencyService.Domain.Models;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using migApp.Shared.Domain.ValueObjects;
using migApp.Shared.Messaging.IntegrationEvents.ExchangeRates;
using ZiggyCreatures.Caching.Fusion;

namespace CurrencyService.Application.Features.Commands.UpdateExchangeRates;

public sealed class UpdateExchangeRatesCommandHandler(
    IAppDbContext context,
    IExchangeRateProvider provider,
    IPublishEndpoint publish,
    IFusionCache cache,
    ICurrencyMetrics metrics,
    TimeProvider timeProvider) : IRequestHandler<UpdateExchangeRatesCommand>
{
    public async Task Handle(UpdateExchangeRatesCommand request, CancellationToken cancellationToken)
    {
        var supported = new[] { Currency.USD, Currency.EUR, Currency.BYN, Currency.RUB };

        var exchangeRates = await context.ExchangeRates
            .Where(x => supported.Contains(x.Currency))
            .ToListAsync(cancellationToken);

        var rates = await provider.GetRatesAsync([..supported.Select(c => c.Code)], cancellationToken);

        var now = timeProvider.GetUtcNow();

        foreach (var (currencyCode, rate) in rates)
        {
            var existing = exchangeRates
                .FirstOrDefault(x => x.Currency.Code == currencyCode);

            if (existing != null)
            {
                existing.Update(rate, now);
                continue;
            }

            var currencyResult = Currency.Create(currencyCode);
            if (currencyResult.IsFailure)
                continue;

            var exchangeRateResult = ExchangeRate.Create(
                currencyResult.Value,
                rate,
                now);

            if (exchangeRateResult.IsSuccess)
                context.ExchangeRates.Add(exchangeRateResult.Value);
        }

        await publish.Publish(
            new CurrenciesUpdatedIntegrationEvent(
                exchangeRates.ToDictionary(r => r.Currency.Code, r => r.Rate)),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordRatesUpdated(rates.Count);
        metrics.RecordSuccessfulUpdate(now);

        foreach (var expected in supported)
        {
            if (!rates.ContainsKey(expected.Code))
                metrics.RecordRateMissing(expected.Code);
        }

        await cache.RemoveByTagAsync(
            CacheTags.ExchangeRateByBaseCurrency(), 
            token: cancellationToken);

        metrics.RecordCacheInvalidation("exchange-rate-by-base-currency");
    }
}
