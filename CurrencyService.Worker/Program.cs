using CurrencyService.Worker;
using CurrencyService.Application.DependencyInjection;
using CurrencyService.Infrastructure.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var host = builder.Build();

await host.MigrateDatabaseAsync();

host.Run();
