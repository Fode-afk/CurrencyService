using CurrencyService.Api.Grpc.V1.Protos;
using CurrencyService.Application.Features.Commands.Convert;
using Grpc.Core;
using MediatR;
using migApp.Shared.Grpc;

namespace CurrencyService.Api.Grpc.V1;

public class GrpcServer(IMediator mediator) : CurrencyServiceV1.CurrencyServiceV1Base
{
    public override async Task<ConvertResponse> Convert(ConvertRequest request, ServerCallContext context)
    {
        var result = await mediator.Send(
            new ConvertCommand(
                request.SourceMoneyMinor,
                request.SourceCurrency,
                request.TargetCurrency),
            context.CancellationToken);
        return new ConvertResponse { ConvertedMoneyMinor = result.ThrowIfFailure() };
    }
}
