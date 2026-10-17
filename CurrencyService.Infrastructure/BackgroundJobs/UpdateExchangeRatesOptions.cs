namespace CurrencyService.Infrastructure.BackgroundJobs;

public sealed class UpdateExchangeRatesOptions
{
    public const string SectionName = "UpdateExchangeRates";

    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(10);
}
