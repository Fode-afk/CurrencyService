using CurrencyService.Api.Grpc.V1.Protos;
using CurrencyService.Application.Features.Queries.GetExchangeRate;
using Grpc.Core;
using MediatR;
using migApp.Shared.Grpc;

namespace CurrencyService.Api.Grpc.V1;

internal sealed class GrpcServer(IMediator mediator) : Protos.CurrencyService.CurrencyServiceBase
{
    public override async Task<GetExchangeRateResponse> GetExchangeRate(GetExchangeRateRequest request, ServerCallContext context)
    {
        var result = await mediator.Send(new GetExchangeRateQuery(request.CurrencyFrom, request.CurrencyTo), context.CancellationToken);
        return new GetExchangeRateResponse { Rate = result.ThrowIfFailure().ToString() };
    }
}
