using MediatR;
using migApp.Shared.Results;

namespace CurrencyService.Application.Features.Commands.Convert;

public sealed record ConvertCommand(
    long SourceMoneyMinor,
    string SourceCurrency,
    string TargetCurrency) : IRequest<IResult<long>>;