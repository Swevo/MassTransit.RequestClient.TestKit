using System.Collections.Concurrent;
using MassTransit;

namespace MassTransit.TestKit;

/// <summary>
/// A fake <see cref="IPublishEndpoint"/> that captures published messages into a thread-safe queue
/// for assertion in unit and integration tests.
/// </summary>
/// <example>
/// <code>
/// var endpoint = new FakePublishEndpoint();
/// await endpoint.Publish(new OrderPlaced { OrderId = 1 });
///
/// endpoint.GetMessages&lt;OrderPlaced&gt;().Should().ContainSingle(m => m.OrderId == 1);
/// </code>
/// </example>
public sealed class FakePublishEndpoint : IPublishEndpoint
{
    private readonly ConcurrentQueue<object> _messages = new();

    // ── Inspection ────────────────────────────────────────────────────────────

    /// <summary>All messages published since the last <see cref="Clear"/>.</summary>
    public IReadOnlyList<object> PublishedMessages => _messages.ToArray();

    /// <summary>Returns all published messages of type <typeparamref name="T"/>.</summary>
    public IReadOnlyList<T> GetMessages<T>() where T : class
        => _messages.OfType<T>().ToList().AsReadOnly();

    /// <summary>Returns the most recently published message of type <typeparamref name="T"/>,
    /// or <c>null</c> if none has been published.</summary>
    public T? MostRecent<T>() where T : class
        => _messages.OfType<T>().LastOrDefault();

    /// <summary>Returns <c>true</c> if at least one message of type <typeparamref name="T"/> was published.</summary>
    public bool WasPublished<T>() where T : class
        => _messages.OfType<T>().Any();

    /// <summary>Clears all captured messages.</summary>
    public void Clear()
    {
        while (_messages.TryDequeue(out _)) { }
    }

    // ── IPublishEndpoint ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Enqueue(message!);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Enqueue(message!);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Enqueue(message!);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish(object message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Enqueue(values);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Enqueue(values);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Enqueue(values);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => NoOpConnectHandle.Instance;

    // ── Private helpers ───────────────────────────────────────────────────────

    private sealed class NoOpConnectHandle : ConnectHandle
    {
        internal static readonly NoOpConnectHandle Instance = new();
        public void Disconnect() { }
        public void Dispose() { }
    }
}
