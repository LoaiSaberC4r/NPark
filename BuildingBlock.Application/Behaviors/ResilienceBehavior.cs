using BuildingBlock.Application.Abstraction;
using MediatR;
using Polly;

namespace BuildingBlock.Application.Behaviors;

public interface IRetryableQuery { }

public sealed class ResilienceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly IAsyncPolicy Retry = Policy.Handle<HttpRequestException>()
        .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt));

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        => request is IRetryableQuery && request is not ICommandRequest
            ? Retry.ExecuteAsync(_ => next(), ct)
            : next();
}
