using MediatR;
using migApp.Shared.Results;

namespace CurrencyService.Application.Features.Queries.GetExchangeRate;

public sealed record GetExchangeRateQuery(
    string CurrencyFrom,
    string CurrencyTo) : IRequest<IResult<decimal>>;