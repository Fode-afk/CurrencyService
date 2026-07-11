using CurrencyService.Application.DependencyInjection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using migApp.Shared.Behaviours;

namespace CurrencyService.Application.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddValidators()
            .AddMediatR()
            .AddTimeProvider();

    private static IServiceCollection AddValidators(this IServiceCollection services) =>
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

    private static IServiceCollection AddMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    private static IServiceCollection AddTimeProvider(this IServiceCollection services) =>
        services.AddSingleton(TimeProvider.System);
}
