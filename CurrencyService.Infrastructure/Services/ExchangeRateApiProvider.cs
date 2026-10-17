using CurrencyService.Application.Interfaces.Metrics;
using CurrencyService.Application.Interfaces.Services;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace CurrencyService.Infrastructure.Services;

internal sealed class ExchangeRateApiProvider(
    HttpClient client,
    ICurrencyMetrics metrics) : IExchangeRateProvider
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Dictionary<string, decimal>> GetRatesAsync(
        string[] currencies,
        CancellationToken cancellationToken = default)
    {
        metrics.RecordProviderCall();
        var sw = Stopwatch.StartNew();

        try
        {
            var rates = await GetAllRates(cancellationToken);

            return rates
                .Where(x => currencies.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }
        catch (Exception ex)
        {
            metrics.RecordProviderFailure(ex.GetType().Name);
            throw;
        }
        finally
        {
            metrics.RecordProviderDuration(sw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<Dictionary<string, decimal>> GetAllRates(
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync("latest.json", cancellationToken);

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>(
            _options,
            cancellationToken)
            ?? throw new JsonException(
                "Exchange rate API returned an empty response.");

        return dto.Rates;
    }

    private sealed record ExchangeRatesResponse(Dictionary<string, decimal> Rates);
}