using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyService.Application.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly))
            .AddSingleton(TimeProvider.System);
}