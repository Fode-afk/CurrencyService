using MediatR;

namespace CurrencyService.Application.Features.Commands.UpdateExchangeRates;

public sealed record UpdateExchangeRatesCommand() : IRequest;