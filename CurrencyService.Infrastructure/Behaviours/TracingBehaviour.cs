using CurrencyService.Application.Interfaces.Metrics;
using MediatR;
using System.Diagnostics;

namespace CurrencyService.Infrastructure.Behaviours;

public sealed class TracingBehaviour<TRequest, TResponse>(
    ICurrencyMetrics metrics)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static readonly ActivitySource Source =
        new("CurrencyService");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var handlerName = typeof(TRequest).Name;

        var spanName = handlerName.Contains("Query")
            ? $"query.{handlerName}"
            : handlerName.Contains("Projection") || handlerName.Contains("Snapshot")
                ? $"projection.{handlerName}"
                : $"command.{handlerName}";

        using var activity = Source.StartActivity(spanName)
            ?.SetTag("handler", handlerName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            metrics.RecordHandlerError(handlerName, spanName[..spanName.IndexOf('.')]);

            throw;
        }
        finally
        {
            metrics.RecordHandlerDuration(
                sw.Elapsed.TotalMilliseconds,
                handlerName,
                spanName[..spanName.IndexOf('.')]);
        }
    }
}