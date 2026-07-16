using CurrencyService.Api.Grpc.V1;
using CurrencyService.Application.DependencyInjection;
using CurrencyService.Infrastructure.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Host.UseSerilog((ctx, services, config) =>
{
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("ServiceName", "CurrencyService")
        .WriteTo.Async(a => a.Console(new JsonFormatter()),
            bufferSize: 10000,
            blockWhenFull: false)
        .WriteTo.Async(a => a.OpenTelemetry(opts =>
        {
            opts.Endpoint = builder.Configuration.GetConnectionString("OtlpEndpoint")
                ?? throw new InvalidOperationException("OtlpEndpoint is not configured");
            opts.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "CurrencyService"
            };
        }),
            bufferSize: 10000,
            blockWhenFull: false);
});

var app = builder.Build();

//await app.MigrateDatabaseAsync();

app.UseHttpsRedirection();

app.MapGrpcService<GrpcServer>();
app.MapGrpcHealthChecksService();

app.Run();