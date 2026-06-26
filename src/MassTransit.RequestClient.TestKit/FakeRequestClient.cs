using System.Collections.Concurrent;
using MassTransit;

namespace MassTransit.TestKit;

/// <summary>
/// A lightweight fake <see cref="IRequestClient{TRequest}"/> for unit testing MassTransit
/// request/response consumers without running the full MassTransit test harness.
/// </summary>
/// <typeparam name="TRequest">The request message type.</typeparam>
/// <example>
/// <code>
/// var client = new FakeRequestClient&lt;GetDevice&gt;()
///     .RespondWith(new DeviceFound { DeviceId = 42 });
///
/// var response = await client.GetResponse&lt;DeviceFound&gt;(new GetDevice { DeviceId = 42 });
///
/// client.WasCalled.Should().BeTrue();
/// client.MostRecentRequest!.DeviceId.Should().Be(42);
/// </code>
/// </example>
public sealed class FakeRequestClient<TRequest> : IRequestClient<TRequest>
    where TRequest : class
{
    private readonly ConcurrentQueue<TRequest> _requests = new();
    private Func<TRequest, object>? _responseFactory;
    private Exception? _exception;

    // ── Fluent configuration ──────────────────────────────────────────────────

    /// <summary>Configures the client to return <paramref name="response"/> for every request.</summary>
    public FakeRequestClient<TRequest> RespondWith<TResponse>(TResponse response) where TResponse : class
    {
        _responseFactory = _ => response;
        _exception = null;
        return this;
    }

    /// <summary>Configures the client to call <paramref name="factory"/> with each received request.</summary>
    public FakeRequestClient<TRequest> RespondWith<TResponse>(Func<TRequest, TResponse> factory) where TResponse : class
    {
        _responseFactory = req => factory(req);
        _exception = null;
        return this;
    }

    /// <summary>Configures the client to throw <paramref name="exception"/> whenever a response is awaited.</summary>
    public FakeRequestClient<TRequest> Throws(Exception exception)
    {
        _exception = exception;
        _responseFactory = null;
        return this;
    }

    // ── Inspection ────────────────────────────────────────────────────────────

    /// <summary>All requests received since the last <see cref="Reset"/> (or construction).</summary>
    public IReadOnlyList<TRequest> ReceivedRequests => _requests.ToArray();

    /// <summary>The most recently received request, or <c>null</c> if no requests have been made.</summary>
    public TRequest? MostRecentRequest => _requests.IsEmpty ? null : _requests.Last();

    /// <summary><c>true</c> if at least one request has been received.</summary>
    public bool WasCalled => !_requests.IsEmpty;

    /// <summary>The total number of requests received.</summary>
    public int CallCount => _requests.Count;

    /// <summary>Clears the recorded request history.</summary>
    public void Reset()
    {
        while (_requests.TryDequeue(out _)) { }
    }

    // ── Public convenience overloads ──────────────────────────────────────────
    // These allow calling GetResponse<T> directly on FakeRequestClient without
    // casting to IRequestClient<TRequest> first, which improves test ergonomics.

    /// <summary>Sends <paramref name="message"/> and returns the configured response.</summary>
    public Task<Response<T>> GetResponse<T>(TRequest message, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
        where T : class => HandleSingle<T>(message);

    /// <summary>Sends <paramref name="message"/> and returns a union response for two types.</summary>
    public Task<Response<T1, T2>> GetResponse<T1, T2>(TRequest message, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
        where T1 : class
        where T2 : class => HandleUnion<T1, T2>(message);

    /// <summary>Sends <paramref name="message"/> and returns a union response for three types.</summary>
    public Task<Response<T1, T2, T3>> GetResponse<T1, T2, T3>(TRequest message, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
        where T1 : class
        where T2 : class
        where T3 : class => HandleUnion<T1, T2, T3>(message);

    // ── IRequestClient<TRequest> ──────────────────────────────────────────────

    RequestHandle<TRequest> IRequestClient<TRequest>.Create(
        TRequest message, CancellationToken cancellationToken, RequestTimeout timeout)
    {
        _requests.Enqueue(message);
        return new FakeRequestHandle<TRequest>(message, _responseFactory, _exception, cancellationToken);
    }

    RequestHandle<TRequest> IRequestClient<TRequest>.Create(
        object values, CancellationToken cancellationToken, RequestTimeout timeout)
        => throw new NotSupportedException(
            $"{nameof(FakeRequestClient<TRequest>)} does not support anonymous-object request creation.");

    // Single response

    Task<Response<T>> IRequestClient<TRequest>.GetResponse<T>(
        TRequest message, CancellationToken cancellationToken, RequestTimeout timeout)
        => HandleSingle<T>(message);

    Task<Response<T>> IRequestClient<TRequest>.GetResponse<T>(
        TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        CancellationToken cancellationToken, RequestTimeout timeout)
        => HandleSingle<T>(message);

    Task<Response<T>> IRequestClient<TRequest>.GetResponse<T>(
        object values, CancellationToken cancellationToken, RequestTimeout timeout)
        => throw new NotSupportedException(
            $"{nameof(FakeRequestClient<TRequest>)} does not support anonymous-object request creation.");

    Task<Response<T>> IRequestClient<TRequest>.GetResponse<T>(
        object values, RequestPipeConfiguratorCallback<TRequest> callback,
        CancellationToken cancellationToken, RequestTimeout timeout)
        => throw new NotSupportedException(
            $"{nameof(FakeRequestClient<TRequest>)} does not support anonymous-object request creation.");

    // Two-response union

    Task<Response<T1, T2>> IRequestClient<TRequest>.GetResponse<T1, T2>(
        TRequest message, CancellationToken cancellationToken, RequestTimeout timeout)
        => HandleUnion<T1, T2>(message);

    Task<Response<T1, T2>> IRequestClient<TRequest>.GetResponse<T1, T2>(
        TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        CancellationToken cancellationToken, RequestTimeout timeout)
        => HandleUnion<T1, T2>(message);

    Task<Response<T1, T2>> IRequestClient<TRequest>.GetResponse<T1, T2>(
        object values, CancellationToken cancellationToken, RequestTimeout timeout)
        => throw new NotSupportedException(
            $"{nameof(FakeRequestClient<TRequest>)} does not support anonymous-object request creation.");

    Task<Response<T1, T2>> IRequestClient<TRequest>.GetResponse<T1, T2>(
        object values, RequestPipeConfiguratorCallback<TRequest> callback,
        CancellationToken cancellationToken, RequestTimeout timeout)
        => throw new NotSupportedException(
            $"{nameof(FakeRequestClient<TRequest>)} does not support anonymous-object request creation.");

    // Three-response union

    Task<Response<T1, T2, T3>> IRequestClient<TRequest>.GetResponse<T1, T2, T3>(
        TRequest message, CancellationToken cancellationToken, RequestTimeout timeout)
        => HandleUnion<T1, T2, T3>(message);

    Task<Response<T1, T2, T3>> IRequestClient<TRequest>.GetResponse<T1, T2, T3>(
        TRequest message, RequestPipeConfiguratorCallback<TRequest> callback,
        CancellationToken cancellationToken, RequestTimeout timeout)
        => HandleUnion<T1, T2, T3>(message);

    Task<Response<T1, T2, T3>> IRequestClient<TRequest>.GetResponse<T1, T2, T3>(
        object values, CancellationToken cancellationToken, RequestTimeout timeout)
        => throw new NotSupportedException(
            $"{nameof(FakeRequestClient<TRequest>)} does not support anonymous-object request creation.");

    Task<Response<T1, T2, T3>> IRequestClient<TRequest>.GetResponse<T1, T2, T3>(
        object values, RequestPipeConfiguratorCallback<TRequest> callback,
        CancellationToken cancellationToken, RequestTimeout timeout)
        => throw new NotSupportedException(
            $"{nameof(FakeRequestClient<TRequest>)} does not support anonymous-object request creation.");

    // ── Private dispatch helpers ──────────────────────────────────────────────

    private Task<Response<T>> HandleSingle<T>(TRequest message) where T : class
    {
        _requests.Enqueue(message);

        if (_exception is not null)
            return Task.FromException<Response<T>>(_exception);

        var obj = GetConfiguredResponse(message);
        if (obj is not T typed)
            return Task.FromException<Response<T>>(new InvalidOperationException(
                $"FakeRequestClient configured response is '{obj?.GetType().Name ?? "null"}', not '{typeof(T).Name}'."));

        return Task.FromResult<Response<T>>(new FakeResponse<T>(typed));
    }

    private Task<Response<T1, T2>> HandleUnion<T1, T2>(TRequest message)
        where T1 : class
        where T2 : class
    {
        _requests.Enqueue(message);

        if (_exception is not null)
            return Task.FromException<Response<T1, T2>>(_exception);

        var obj = GetConfiguredResponse(message);

        if (obj is T1 t1)
        {
            return Task.FromResult(new Response<T1, T2>(
                Task.FromResult<Response<T1>>(new FakeResponse<T1>(t1)),
                new TaskCompletionSource<Response<T2>>().Task));
        }

        if (obj is T2 t2)
        {
            return Task.FromResult(new Response<T1, T2>(
                new TaskCompletionSource<Response<T1>>().Task,
                Task.FromResult<Response<T2>>(new FakeResponse<T2>(t2))));
        }

        return Task.FromException<Response<T1, T2>>(new InvalidOperationException(
            $"FakeRequestClient configured response is '{obj?.GetType().Name ?? "null"}', " +
            $"which is neither '{typeof(T1).Name}' nor '{typeof(T2).Name}'."));
    }

    private Task<Response<T1, T2, T3>> HandleUnion<T1, T2, T3>(TRequest message)
        where T1 : class
        where T2 : class
        where T3 : class
    {
        _requests.Enqueue(message);

        if (_exception is not null)
        {
            return Task.FromResult(new Response<T1, T2, T3>(
                Task.FromException<Response<T1>>(_exception),
                Task.FromException<Response<T2>>(_exception),
                Task.FromException<Response<T3>>(_exception)));
        }

        var obj = GetConfiguredResponse(message);

        if (obj is T1 t1)
        {
            return Task.FromResult(new Response<T1, T2, T3>(
                Task.FromResult<Response<T1>>(new FakeResponse<T1>(t1)),
                new TaskCompletionSource<Response<T2>>().Task,
                new TaskCompletionSource<Response<T3>>().Task));
        }

        if (obj is T2 t2)
        {
            return Task.FromResult(new Response<T1, T2, T3>(
                new TaskCompletionSource<Response<T1>>().Task,
                Task.FromResult<Response<T2>>(new FakeResponse<T2>(t2)),
                new TaskCompletionSource<Response<T3>>().Task));
        }

        if (obj is T3 t3)
        {
            return Task.FromResult(new Response<T1, T2, T3>(
                new TaskCompletionSource<Response<T1>>().Task,
                new TaskCompletionSource<Response<T2>>().Task,
                Task.FromResult<Response<T3>>(new FakeResponse<T3>(t3))));
        }

        return Task.FromException<Response<T1, T2, T3>>(new InvalidOperationException(
            $"FakeRequestClient configured response is '{obj?.GetType().Name ?? "null"}', " +
            $"which is none of '{typeof(T1).Name}', '{typeof(T2).Name}', '{typeof(T3).Name}'."));
    }

    private object GetConfiguredResponse(TRequest message)
    {
        if (_responseFactory is null)
            throw new InvalidOperationException(
                "No response configured on FakeRequestClient. Call RespondWith() before sending a request.");

        return _responseFactory(message);
    }
}
