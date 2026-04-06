using CurrencyService.Api.Grpc.V1;
using CurrencyService.Application.DependencyInjection;
using CurrencyService.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGrpcService<GrpcServer>();

app.Run();