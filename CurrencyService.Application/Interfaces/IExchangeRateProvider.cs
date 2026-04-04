namespace CurrencyService.Application.Interfaces;

public interface IExchangeRateProvider
{
    Task<decimal?> GetRateAsync(string currency, CancellationToken cancellationToken = default);
    Task<Dictionary<string, decimal>> GetRatesAsync(string[] currencies, CancellationToken cancellationToken = default);
}
