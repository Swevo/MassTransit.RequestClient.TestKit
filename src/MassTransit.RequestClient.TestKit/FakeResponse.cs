using System.Collections;
using MassTransit;

namespace MassTransit.TestKit;

/// <summary>
/// Minimal implementation of <see cref="Response{TResponse}"/> for test assertions.
/// Only <see cref="Message"/> is populated; all <c>MessageContext</c> members return null/defaults.
/// </summary>
internal sealed class FakeResponse<T> : Response<T> where T : class
{
    private static readonly Headers _emptyHeaders = new EmptyHeaders();

    internal FakeResponse(T message) => Message = message;

    /// <inheritdoc/>
    public T Message { get; }

    object Response.Message => Message;

    Guid? MessageContext.MessageId => null;
    Guid? MessageContext.RequestId => null;
    Guid? MessageContext.CorrelationId => null;
    Guid? MessageContext.ConversationId => null;
    Guid? MessageContext.InitiatorId => null;
    DateTime? MessageContext.ExpirationTime => null;
    Uri MessageContext.SourceAddress => null!;
    Uri MessageContext.DestinationAddress => null!;
    Uri MessageContext.ResponseAddress => null!;
    Uri MessageContext.FaultAddress => null!;
    DateTime? MessageContext.SentTime => null;
    Headers MessageContext.Headers => _emptyHeaders;
    HostInfo MessageContext.Host => null!;

    private sealed class EmptyHeaders : Headers
    {
        IEnumerable<KeyValuePair<string, object>> Headers.GetAll()
            => Enumerable.Empty<KeyValuePair<string, object>>();

        bool Headers.TryGetHeader(string key, out object value) { value = null!; return false; }

        T Headers.Get<T>(string key, T defaultValue) => defaultValue;

        // Explicit impl for the struct-constrained Nullable<T> overload.
        Nullable<T> Headers.Get<T>(string key, Nullable<T> defaultValue) where T : struct => defaultValue;

        IEnumerator<HeaderValue> IEnumerable<HeaderValue>.GetEnumerator()
            => Enumerable.Empty<HeaderValue>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Enumerable.Empty<HeaderValue>().GetEnumerator();
    }
}
