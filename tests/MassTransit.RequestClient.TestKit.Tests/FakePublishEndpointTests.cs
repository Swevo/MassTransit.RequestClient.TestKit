public record OrderPlaced(Guid OrderId, decimal Amount);
public record DeviceStatusChanged(int DeviceId, string Status);

/// <summary>Tests for <see cref="FakePublishEndpoint"/>.</summary>
public class FakePublishEndpointTests
{
    private readonly FakePublishEndpoint _endpoint = new();

    // ── Publish and capture ───────────────────────────────────────────────────

    [Fact]
    public async Task Publish_SingleMessage_CapturesIt()
    {
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 49.99m));

        _endpoint.PublishedMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task Publish_MultipleMessages_CapturesAll()
    {
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 10m));
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 20m));
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 30m));

        _endpoint.PublishedMessages.Should().HaveCount(3);
    }

    // ── GetMessages<T> ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMessages_FiltersByType()
    {
        var orderId = Guid.NewGuid();
        await _endpoint.Publish(new OrderPlaced(orderId, 99m));
        await _endpoint.Publish(new DeviceStatusChanged(7, "Online"));
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 1m));

        var orders = _endpoint.GetMessages<OrderPlaced>();

        orders.Should().HaveCount(2);
        orders.Should().Contain(o => o.OrderId == orderId);
    }

    [Fact]
    public async Task GetMessages_NoMatchingType_ReturnsEmpty()
    {
        await _endpoint.Publish(new DeviceStatusChanged(1, "Offline"));

        _endpoint.GetMessages<OrderPlaced>().Should().BeEmpty();
    }

    // ── WasPublished<T> ───────────────────────────────────────────────────────

    [Fact]
    public async Task WasPublished_AfterPublish_ReturnsTrue()
    {
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 5m));

        _endpoint.WasPublished<OrderPlaced>().Should().BeTrue();
    }

    [Fact]
    public void WasPublished_NothingPublished_ReturnsFalse()
        => _endpoint.WasPublished<OrderPlaced>().Should().BeFalse();

    // ── MostRecent<T> ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MostRecent_ReturnsLastMessageOfType()
    {
        var first = Guid.NewGuid();
        var last = Guid.NewGuid();
        await _endpoint.Publish(new OrderPlaced(first, 1m));
        await _endpoint.Publish(new DeviceStatusChanged(3, "Rebooting"));
        await _endpoint.Publish(new OrderPlaced(last, 2m));

        _endpoint.MostRecent<OrderPlaced>()!.OrderId.Should().Be(last);
    }

    [Fact]
    public void MostRecent_NothingPublished_ReturnsNull()
        => _endpoint.MostRecent<OrderPlaced>().Should().BeNull();

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Clear_RemovesAllMessages()
    {
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 1m));
        await _endpoint.Publish(new OrderPlaced(Guid.NewGuid(), 2m));

        _endpoint.Clear();

        _endpoint.PublishedMessages.Should().BeEmpty();
        _endpoint.WasPublished<OrderPlaced>().Should().BeFalse();
    }

    // ── Typed Publish overloads ───────────────────────────────────────────────

    [Fact]
    public async Task PublishTyped_WithObject_CapturesMessage()
    {
        await _endpoint.Publish((object)new OrderPlaced(Guid.NewGuid(), 1m));

        _endpoint.PublishedMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task PublishTyped_WithTypeParameter_CapturesMessage()
    {
        await _endpoint.Publish<OrderPlaced>(new OrderPlaced(Guid.NewGuid(), 99m));

        _endpoint.WasPublished<OrderPlaced>().Should().BeTrue();
    }
}
