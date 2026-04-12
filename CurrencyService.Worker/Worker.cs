using CurrencyService.Application.Features.Commands.UpdateExchangeRates;
using MediatR;

namespace CurrencyService.Worker;

internal sealed class Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var result = await mediator.Send(new UpdateExchangeRatesCommand(), cancellationToken: stoppingToken);

                if (result.IsFailure)
                    logger.LogError("Failed to update exchange rates");
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
