using CurrencyService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CurrencyService.Infrastructure.BackgroundJobs;

internal sealed class ExchangeRateBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExchangeRateBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var exchangeRateService = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();

                await exchangeRateService.UpdateExchangeRatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update exchange rates");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(Random.Shared.Next(0, 30)),
                stoppingToken);
        }
    }
}
