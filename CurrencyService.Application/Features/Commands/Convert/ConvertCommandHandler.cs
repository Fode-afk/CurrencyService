using CurrencyService.Application.Interfaces;
using MediatR;
using migApp.Shared.Domain.Errors;
using migApp.Shared.Domain.ValueObjects;
using migApp.Shared.Results;
using static migApp.Shared.Results.ResultFactory;

namespace CurrencyService.Application.Features.Commands.Convert;

public sealed class ConvertCommandHandler(IExchangeRateService exchangeRateService) : IRequestHandler<ConvertCommand, IResult<long>>
{
    public async Task<IResult<long>> Handle(ConvertCommand request, CancellationToken cancellationToken)
    {
        var sourceCurrencyResult = Currency.Create(request.SourceCurrency);
        if (sourceCurrencyResult.IsFailure)
            return Fail<long>(sourceCurrencyResult.Error);

        var targetCurrencyResult = Currency.Create(request.TargetCurrency);
        if (targetCurrencyResult.IsFailure)
            return Fail<long>(targetCurrencyResult.Error);

        var sourceCurrency = sourceCurrencyResult.Value;
        var targetCurrency = targetCurrencyResult.Value;

        if (sourceCurrency == targetCurrency)
            return Ok(request.SourceMoneyMinor);

        var exchangeRate = await exchangeRateService.GetExchangeRateAsync(sourceCurrency, targetCurrency, cancellationToken);

        if (exchangeRate == null)
            return Fail<long>(MoneyErrors.ExchangeRateNotFound());

        var sourceMoneyResult = Money.FromMinor(request.SourceMoneyMinor, sourceCurrency);
        if (sourceMoneyResult.IsFailure)
            return Fail<long>(sourceMoneyResult.Error);

        var convertedAmount = Math.Round(
            sourceMoneyResult.Value.Amount * exchangeRate.Value,
            targetCurrency.MinorUnits,
            MidpointRounding.ToEven);

        return Money.ToMinor(convertedAmount, targetCurrency);
    }
}
