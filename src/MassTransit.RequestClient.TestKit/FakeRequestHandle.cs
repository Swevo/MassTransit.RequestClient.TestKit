using MassTransit;
using MassTransit.Configuration;

namespace MassTransit.TestKit;

/// <summary>
/// Fake <see cref="RequestHandle{TRequest}"/> returned by <see cref="FakeRequestClient{TRequest}.Create"/>.
/// Routes <c>GetResponse&lt;T&gt;</c> to the configured response factory.
/// </summary>
public sealed class FakeRequestHandle<TRequest>(
    TRequest message,
    Func<TRequest, object>? responseFactory,
    Exception? exception,
    CancellationToken cancellationToken)
    : RequestHandle<TRequest>
    where TRequest : class
{
    private readonly Guid _requestId = Guid.NewGuid();

    // ── RequestHandle<TRequest> ───────────────────────────────────────────────

    Task<TRequest> RequestHandle<TRequest>.Message => Task.FromResult(message);

    // ── RequestHandle (base) ──────────────────────────────────────────────────

    Task<Response<T>> RequestHandle.GetResponse<T>(bool readyToSend)
    {
        if (exception is not null)
            return Task.FromException<Response<T>>(exception);

        var obj = GetResponseObject();
        if (obj is not T typed)
            return Task.FromException<Response<T>>(new InvalidOperationException(
                $"FakeRequestClient configured response is '{obj?.GetType().Name ?? "null"}', not '{typeof(T).Name}'."));

        return Task.FromResult<Response<T>>(new FakeResponse<T>(typed));
    }

    void RequestHandle.Cancel() { }

    // ── IRequestPipeConfigurator ──────────────────────────────────────────────

    Guid IRequestPipeConfigurator.RequestId => _requestId;

    RequestTimeout IRequestPipeConfigurator.TimeToLive { set { } }

    // ── IPipeConfigurator<SendContext<TRequest>> ──────────────────────────────

    void IPipeConfigurator<SendContext<TRequest>>.AddPipeSpecification(
        IPipeSpecification<SendContext<TRequest>> specification) { }

    // ── IDisposable ───────────────────────────────────────────────────────────

    void IDisposable.Dispose() { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private object GetResponseObject()
    {
        if (responseFactory is null)
            throw new InvalidOperationException(
                "No response configured on FakeRequestClient. Call RespondWith() before sending a request.");

        return responseFactory(message);
    }
}
