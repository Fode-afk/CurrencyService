using CurrencyService.Application.Interfaces.Metrics;
using System.Diagnostics.Metrics;

namespace CurrencyService.Infrastructure.Observability;

public sealed class CurrencyServiceMetrics : IDisposable, ICurrencyMetrics
{
    public const string MeterName = "CurrencyService";
    private readonly Meter _meter;

    private readonly Counter<long> _handlerErrors;
    private readonly Histogram<double> _handlerDuration;

    private readonly Counter<long> _exchangeRateProviderCalls;
    private readonly Counter<long> _exchangeRateProviderFailures;
    private readonly Histogram<double> _exchangeRateProviderDuration;

    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _cacheInvalidations;

    private readonly Counter<long> _ratesUpdated;
    private readonly Counter<long> _ratesMissing;

    private long _lastSuccessfulUpdateUnixSeconds;
    private readonly ObservableGauge<long> _secondsSinceLastSuccessfulUpdate;

    public CurrencyServiceMetrics()
    {
        _meter = new Meter(MeterName);

        _handlerErrors = _meter.CreateCounter<long>("currency.handlers.errors");
        _handlerDuration = _meter.CreateHistogram<double>("currency.handlers.duration", unit: "ms");

        _exchangeRateProviderCalls = _meter.CreateCounter<long>(
            "currency.provider.calls",
            description: "Calls made to the external exchange rate API");

        _exchangeRateProviderFailures = _meter.CreateCounter<long>(
            "currency.provider.failures",
            description: "Failed calls to the external exchange rate API");

        _exchangeRateProviderDuration = _meter.CreateHistogram<double>(
            "currency.provider.duration",
            unit: "ms");

        _cacheHits = _meter.CreateCounter<long>("currency.cache.hits");
        _cacheMisses = _meter.CreateCounter<long>("currency.cache.misses");
        _cacheInvalidations = _meter.CreateCounter<long>("currency.cache.invalidations");

        _ratesUpdated = _meter.CreateCounter<long>(
            "currency.rates.updated",
            description: "Number of currency rates successfully updated per refresh cycle");

        _ratesMissing = _meter.CreateCounter<long>(
            "currency.rates.missing",
            description: "Supported currencies not returned by the external provider");

        _secondsSinceLastSuccessfulUpdate = _meter.CreateObservableGauge(
            "currency.rates.staleness",
            () => _lastSuccessfulUpdateUnixSeconds == 0
                ? 0
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _lastSuccessfulUpdateUnixSeconds,
            unit: "s",
            description: "Seconds since exchange rates were last successfully refreshed");
    }

    public void RecordHandlerError(string handlerName, string handlerType) =>
        _handlerErrors.Add(1,
            new KeyValuePair<string, object?>("handler", handlerName),
            new KeyValuePair<string, object?>("type", handlerType));

    public void RecordHandlerDuration(double ms, string handlerName, string handlerType) =>
        _handlerDuration.Record(ms,
            new KeyValuePair<string, object?>("handler", handlerName),
            new KeyValuePair<string, object?>("type", handlerType));

    public void RecordProviderCall() => _exchangeRateProviderCalls.Add(1);

    public void RecordProviderFailure(string reason) =>
        _exchangeRateProviderFailures.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordProviderDuration(double ms) => _exchangeRateProviderDuration.Record(ms);

    public void RecordCacheHitOrMiss(string cacheKeyPrefix, bool wasHit)
    {
        if (wasHit)
            _cacheHits.Add(1, new KeyValuePair<string, object?>("key_prefix", cacheKeyPrefix));
        else
            _cacheMisses.Add(1, new KeyValuePair<string, object?>("key_prefix", cacheKeyPrefix));
    }

    public void RecordCacheInvalidation(string tag) =>
        _cacheInvalidations.Add(1, new KeyValuePair<string, object?>("tag_type", tag));

    public void RecordRatesUpdated(int count) => _ratesUpdated.Add(count);

    public void RecordRateMissing(string currencyCode) =>
        _ratesMissing.Add(1, new KeyValuePair<string, object?>("currency", currencyCode));

    public void RecordSuccessfulUpdate(DateTimeOffset now) =>
        _lastSuccessfulUpdateUnixSeconds = now.ToUnixTimeSeconds();

    public void Dispose() => _meter.Dispose();
}