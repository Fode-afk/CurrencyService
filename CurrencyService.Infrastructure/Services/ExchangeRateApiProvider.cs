using CurrencyService.Application.Interfaces.Metrics;
using CurrencyService.Application.Interfaces.Services;
using System.Diagnostics;
using System.Text.Json;

namespace CurrencyService.Infrastructure.Services;

internal sealed class ExchangeRateApiProvider(
    IHttpClientFactory factory,
    ICurrencyMetrics metrics) : IExchangeRateProvider
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Dictionary<string, decimal>> GetRatesAsync(string[] currencies, CancellationToken cancellationToken = default)
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

    private async Task<Dictionary<string, decimal>> GetAllRates(CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync("https://cdn.moneyconvert.net/api/latest.json", cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Exchange API failed");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var dto = JsonSerializer.Deserialize<ExchangeRatesResponse>(json, _options)
            ?? throw new JsonException();

        return dto.Rates;
    }

    private sealed record ExchangeRatesResponse(Dictionary<string, decimal> Rates);
}
