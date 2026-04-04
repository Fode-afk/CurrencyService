using CurrencyService.Application.Interfaces;
using CurrencyService.Infrastructure.BackgroundJobs;
using CurrencyService.Infrastructure.Data;
using CurrencyService.Infrastructure.DependencyInjection;
using CurrencyService.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using migApp.Shared.Caching;
using migApp.Shared.Grpc;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace CurrencyService.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddHttpClient()
            .AddServices()
            .AddCache(configuration)
            .AddGrpc();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IExchangeRateRepository, CachedExchangeRateRepository>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<IExchangeRateProvider, ExchangeRateApiProvider>();

        services.AddHostedService<ExchangeRateBackgroundService>();

        return services;
    }

    private static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis")
           ?? throw new InvalidOperationException("Redis connection string is missing");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "CurrencyService:";
        });

        services
            .AddFusionCache()
            .WithOptions(options =>
            {
                options.DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(5),

                    IsFailSafeEnabled = false,

                    AllowBackgroundDistributedCacheOperations = true,
                    AllowBackgroundBackplaneOperations = true,

                    SkipBackplaneNotifications = false,
                    JitterMaxDuration = TimeSpan.Zero,

                    FactorySoftTimeout = TimeSpan.FromMilliseconds(300),
                    FactoryHardTimeout = TimeSpan.FromSeconds(3)
                };
            })
            .WithDistributedCache(sp =>
                sp.GetRequiredService<IDistributedCache>())
            .WithBackplane(sp => new RedisBackplane(
                new RedisBackplaneOptions
                {
                    Configuration = redisConnection
                }))
            .WithSerializer(new JsonFusionCacheSerializer())
            .TryWithAutoSetup();

        return services;
    }

    private static IServiceCollection AddGrpc(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        { 
            options.Interceptors.Add<GrpcExceptionInterceptor>(); 
        });

        return services;
    }
}
