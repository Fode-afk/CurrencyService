namespace CurrencyService.Application.Interfaces.Services;

public interface IExchangeRateProvider
{
    Task<Dictionary<string, decimal>> GetRatesAsync(string[] currencies, CancellationToken cancellationToken = default);
}