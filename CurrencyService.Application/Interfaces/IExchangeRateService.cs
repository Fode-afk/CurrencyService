using migApp.Shared.Domain.ValueObjects;

namespace CurrencyService.Application.Interfaces;

public interface IExchangeRateService
{
    public Task<decimal?> GetExchangeRateAsync(
        Currency from,
        Currency to,
        CancellationToken cancellationToken = default);

    public Task UpdateExchangeRatesAsync(CancellationToken cancellationToken = default);
}
