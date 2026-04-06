using CurrencyService.Api.Grpc.V1.Protos;
using CurrencyService.Application.Features.Commands.Convert;
using Grpc.Core;
using MediatR;
using migApp.Shared.Grpc;
using Proto = CurrencyService.Api.Grpc.V1.Protos;

namespace CurrencyService.Api.Grpc.V1;

public class GrpcServer(IMediator mediator) : Proto.CurrencyService.CurrencyServiceBase
{
    public override async Task<ConvertResponse> Convert(ConvertRequest request, ServerCallContext context)
    {
        var result = await mediator.Send(
            new ConvertCommand(
                request.SourceMoney.AmountMinor,
                request.SourceMoney.Currency,
                request.TargetCurrency),
            context.CancellationToken);

        return new ConvertResponse 
        { 
            ConvertedMoney = new Money
            { 
                AmountMinor = result.ThrowIfFailure(),
                Currency = request.TargetCurrency
            }
        };
    }
}
