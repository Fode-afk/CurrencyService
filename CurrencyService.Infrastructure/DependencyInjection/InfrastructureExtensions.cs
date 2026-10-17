using CurrencyService.Application.Interfaces.Data;
using CurrencyService.Application.Interfaces.Metrics;
using CurrencyService.Application.Interfaces.Services;
using CurrencyService.Infrastructure.BackgroundJobs;
using CurrencyService.Infrastructure.Behaviours;
using CurrencyService.Infrastructure.Data;
using CurrencyService.Infrastructure.Messaging;
using CurrencyService.Infrastructure.Observability;
using CurrencyService.Infrastructure.Services;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using migApp.Shared.Behaviours;
using migApp.Shared.Caching;
using migApp.Shared.Grpc;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using ZiggyCreatures.Caching.Fusion;

namespace CurrencyService.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices(configuration)
            .AddDatabase(configuration)
            .AddCache(configuration)
            .AddGrpcServices()
            .AddHealth(configuration)
            .AddMessaging()
            .AddObservability(configuration)
            .AddBehaviours();

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["ExchangeRateApi:BaseUrl"]
            ?? throw new InvalidOperationException("ExchangeRateApi:BaseUrl is missing.");

        services.AddHttpClient<IExchangeRateProvider, ExchangeRateApiProvider>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddStandardResilienceHandler();

        services.AddOptions<UpdateExchangeRatesOptions>()
            .Bind(configuration.GetSection(UpdateExchangeRatesOptions.SectionName))
            .Validate(o => o.Interval > TimeSpan.Zero, "Interval must be positive")
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<UpdateExchangeRatesOptions>>().Value);
        services.AddHostedService<UpdateExchangeRatesService>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", Schemas.ExchangeRates);
                sqlOptions.EnableRetryOnFailure();
            }));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

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

                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(24),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(30),

                    AllowBackgroundDistributedCacheOperations = true,
                    AllowBackgroundBackplaneOperations = true,
                    SkipBackplaneNotifications = false,

                    JitterMaxDuration = TimeSpan.Zero,

                    FactorySoftTimeout = TimeSpan.FromMilliseconds(300),
                    FactoryHardTimeout = TimeSpan.FromSeconds(3)
                };
            })
            .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>())
            .WithStackExchangeRedisBackplane(o => o.Configuration = redisConnection)
            .WithSerializer(new JsonFusionCacheSerializer());

        return services;
    }

    private static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<GrpcExceptionInterceptor>();
        });

        return services;
    }

    private static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseConnectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing");

        var rabbitMq = configuration
            .GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>()
                ?? throw new InvalidOperationException("RabbitMQ configuration is missing.");

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is missing");

        services.AddGrpcHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"))
            .AddSqlServer(
                connectionString: databaseConnectionString,
                name: "mssql",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"])
            .AddRabbitMQ(
                factory: async sp =>
                {
                    var factory = new ConnectionFactory()
                    {
                        HostName = rabbitMq.Host,
                        Port = rabbitMq.Port,
                        VirtualHost = rabbitMq.VirtualHost,
                        UserName = rabbitMq.Username,
                        Password = rabbitMq.Password
                    };
                    return await factory.CreateConnectionAsync();
                },
                name: "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"])
            .AddRedis(
                redisConnectionString: redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services
           .AddOptions<RabbitMqOptions>()
           .BindConfiguration(RabbitMqOptions.SectionName)
           .Validate(
               options => !string.IsNullOrWhiteSpace(options.Host),
               "RabbitMQ:Host is required.")
           .Validate(
               options => options.Port > 0,
               "RabbitMQ:Port must be greater than zero.")
           .Validate(
               options => !string.IsNullOrWhiteSpace(options.Username),
               "RabbitMQ:Username is required.")
           .Validate(
               options => !string.IsNullOrWhiteSpace(options.Password),
               "RabbitMQ:Password is required.")
           .ValidateOnStart();

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("currency-service", false));         

            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                var options = context
                    .GetRequiredService<IOptions<RabbitMqOptions>>()
                    .Value;

                cfg.Host(options.Host,
                    options.Port,
                    options.VirtualHost,
                    host =>
                    {
                        host.Username(options.Username);
                        host.Password(options.Password);
                    });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Otlp:Endpoint"]
            ?? throw new InvalidOperationException("OtlpEndpoint is not configured");

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("CurrencyService"))
            .UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(otlpEndpoint))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                    opts.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddFusionCacheInstrumentation()
                .AddSource("MassTransit")
                .AddSource("CurrencyService"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddFusionCacheInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("MassTransit")
                .AddMeter(CurrencyServiceMetrics.MeterName));

        services.AddSingleton<ICurrencyMetrics, CurrencyServiceMetrics>();

        return services;
    }

    private static IServiceCollection AddBehaviours(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
