using MediatR;
using migApp.Shared.Results;

namespace CurrencyService.Application.Features.Commands.UpdateExchangeRates;

public sealed record UpdateExchangeRatesCommand() : IRequest<IResult>;