using CurrencyService.Application.Features.Commands.UpdateExchangeRates;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CurrencyService.Infrastructure.BackgroundJobs;

internal sealed class UpdateExchangeRatesService(
    IServiceScopeFactory scopeFactory,
    ILogger<UpdateExchangeRatesService> logger,
    TimeProvider timeProvider,
    UpdateExchangeRatesOptions options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {          
            await UpdateExchangeRatesAsync(stoppingToken);

            using var timer = new PeriodicTimer(options.Interval, timeProvider);

            while (await timer.WaitForNextTickAsync(stoppingToken))
                await UpdateExchangeRatesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task UpdateExchangeRatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new UpdateExchangeRatesCommand(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update exchange rates");
        }
    }
}
