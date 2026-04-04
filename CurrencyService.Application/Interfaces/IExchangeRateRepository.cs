namespace CurrencyService.Application.Interfaces;

public interface IExchangeRateRepository
{
    Task<decimal?> GetAsync(string currency, CancellationToken cancellationToken = default);
    Task SetAsync(string currency, decimal rate, CancellationToken cancellationToken = default);
}