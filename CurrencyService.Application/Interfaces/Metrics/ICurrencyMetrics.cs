namespace CurrencyService.Application.Interfaces.Metrics;

public interface ICurrencyMetrics
{
    void RecordHandlerError(string handlerName, string handlerType);
    void RecordHandlerDuration(double ms, string handlerName, string handlerType);
    void RecordProviderCall();
    void RecordProviderFailure(string reason);
    void RecordProviderDuration(double ms);
    void RecordCacheHitOrMiss(string cacheKeyPrefix, bool wasHit);
    void RecordCacheInvalidation(string tag);
    void RecordRatesUpdated(int count);
    void RecordRateMissing(string currencyCode);
    void RecordSuccessfulUpdate(DateTimeOffset now);
}