using CurrencyService.Api.Grpc.V1;
using CurrencyService.Application.DependencyInjection;
using CurrencyService.Infrastructure.DependencyInjection;
using migApp.Shared.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseMigAppSerilog("CurrencyService");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

//await app.MigrateDatabaseAsync();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.MapGrpcService<GrpcServer>();
app.MapGrpcHealthChecksService();

app.Run();