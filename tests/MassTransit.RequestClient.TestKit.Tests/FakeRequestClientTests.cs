/// <summary>Tests for <see cref="FakeRequestClient{TRequest}"/>.</summary>
public class FakeRequestClientTests
{
    // ── RespondWith (static response) ─────────────────────────────────────────

    [Fact]
    public async Task RespondWith_SingleResponse_ReturnsConfiguredMessage()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(42, "Printer-01"));

        var response = await client.GetResponse<DeviceFound>(new GetDevice(42));

        response.Message.DeviceId.Should().Be(42);
        response.Message.Name.Should().Be("Printer-01");
    }

    [Fact]
    public async Task RespondWith_SingleResponse_RecordsRequest()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(42, "Printer-01"));

        await client.GetResponse<DeviceFound>(new GetDevice(42));

        client.WasCalled.Should().BeTrue();
        client.CallCount.Should().Be(1);
        client.ReceivedRequests.Should().ContainSingle(r => r.DeviceId == 42);
    }

    [Fact]
    public async Task RespondWith_CalledMultipleTimes_RecordsAllRequests()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(1, "A"));

        await client.GetResponse<DeviceFound>(new GetDevice(1));
        await client.GetResponse<DeviceFound>(new GetDevice(2));
        await client.GetResponse<DeviceFound>(new GetDevice(3));

        client.CallCount.Should().Be(3);
        client.ReceivedRequests.Select(r => r.DeviceId).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task MostRecentRequest_ReturnsLastRequest()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(1, "A"));

        await client.GetResponse<DeviceFound>(new GetDevice(1));
        await client.GetResponse<DeviceFound>(new GetDevice(99));

        client.MostRecentRequest!.DeviceId.Should().Be(99);
    }

    // ── RespondWith (factory) ─────────────────────────────────────────────────

    [Fact]
    public async Task RespondWith_Factory_InvokesFactoryWithRequest()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(req => new DeviceFound(req.DeviceId, $"Device-{req.DeviceId}"));

        var response = await client.GetResponse<DeviceFound>(new GetDevice(7));

        response.Message.Name.Should().Be("Device-7");
    }

    [Fact]
    public async Task RespondWith_Factory_DifferentRequestsProduceDifferentResponses()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(req => new DeviceFound(req.DeviceId, $"Dev-{req.DeviceId}"));

        var r1 = await client.GetResponse<DeviceFound>(new GetDevice(1));
        var r2 = await client.GetResponse<DeviceFound>(new GetDevice(2));

        r1.Message.Name.Should().Be("Dev-1");
        r2.Message.Name.Should().Be("Dev-2");
    }

    // ── Throws ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Throws_PropagatesException()
    {
        var client = new FakeRequestClient<GetDevice>()
            .Throws(new TimeoutException("Bus timeout"));

        var act = async () => await client.GetResponse<DeviceFound>(new GetDevice(1));

        await act.Should().ThrowAsync<TimeoutException>().WithMessage("Bus timeout");
    }

    [Fact]
    public async Task Throws_StillRecordsTheRequest()
    {
        var client = new FakeRequestClient<GetDevice>()
            .Throws(new TimeoutException());

        try { await client.GetResponse<DeviceFound>(new GetDevice(5)); } catch { }

        client.WasCalled.Should().BeTrue();
        client.MostRecentRequest!.DeviceId.Should().Be(5);
    }

    // ── No configuration ──────────────────────────────────────────────────────

    [Fact]
    public async Task NoConfiguration_ThrowsInvalidOperationException()
    {
        var client = new FakeRequestClient<GetDevice>();

        var act = async () => await client.GetResponse<DeviceFound>(new GetDevice(1));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RespondWith*");
    }

    // ── Wrong response type ───────────────────────────────────────────────────

    [Fact]
    public async Task WrongResponseType_ThrowsInvalidOperationException()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(1, "A"));

        var act = async () => await client.GetResponse<DeviceNotFound>(new GetDevice(1));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DeviceFound*DeviceNotFound*");
    }

    // ── Union responses (GetResponse<T1, T2>) ─────────────────────────────────

    [Fact]
    public async Task UnionResponse_FirstType_FirstTaskCompletes()
    {
        var client = new FakeRequestClient<PlaceOrder>()
            .RespondWith(new OrderAccepted(Guid.NewGuid()));

        var (accepted, rejected) = await client.GetResponse<OrderAccepted, OrderRejected>(
            new PlaceOrder(Guid.NewGuid(), 99.99m));

        accepted.IsCompletedSuccessfully.Should().BeTrue();
        var response = await accepted;
        response.Message.Should().BeOfType<OrderAccepted>();
    }

    [Fact]
    public async Task UnionResponse_SecondType_SecondTaskCompletes()
    {
        var client = new FakeRequestClient<PlaceOrder>()
            .RespondWith(new OrderRejected(Guid.NewGuid(), "Insufficient funds"));

        var (accepted, rejected) = await client.GetResponse<OrderAccepted, OrderRejected>(
            new PlaceOrder(Guid.NewGuid(), 99.99m));

        rejected.IsCompletedSuccessfully.Should().BeTrue();
        var response = await rejected;
        response.Message.Reason.Should().Be("Insufficient funds");
    }

    [Fact]
    public async Task UnionResponse_FirstType_SecondTaskNeverCompletes()
    {
        var client = new FakeRequestClient<PlaceOrder>()
            .RespondWith(new OrderAccepted(Guid.NewGuid()));

        var (_, rejected) = await client.GetResponse<OrderAccepted, OrderRejected>(
            new PlaceOrder(Guid.NewGuid(), 99.99m));

        rejected.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task UnionResponse_ThrowsConfiguration_OuterTaskFaults()
    {
        var client = new FakeRequestClient<PlaceOrder>()
            .Throws(new TimeoutException());

        Func<Task> act = () => client.GetResponse<OrderAccepted, OrderRejected>(
            new PlaceOrder(Guid.NewGuid(), 99.99m));

        await act.Should().ThrowAsync<TimeoutException>();
    }

    // ── WasCalled when no requests made ──────────────────────────────────────

    [Fact]
    public void WasCalled_NoRequestsMade_ReturnsFalse()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(1, "A"));

        client.WasCalled.Should().BeFalse();
        client.CallCount.Should().Be(0);
        client.MostRecentRequest.Should().BeNull();
    }

    // ── Response.Message ─────────────────────────────────────────────────────

    [Fact]
    public async Task Response_Message_IsAccessible()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(10, "Scanner"));

        var response = await client.GetResponse<DeviceFound>(new GetDevice(10));

        response.Message.DeviceId.Should().Be(10);
        response.Message.Name.Should().Be("Scanner");
    }

    [Fact]
    public async Task Response_NullContextMembers_DoNotThrow()
    {
        var client = new FakeRequestClient<GetDevice>()
            .RespondWith(new DeviceFound(1, "A"));

        var response = await client.GetResponse<DeviceFound>(new GetDevice(1));

        // Accessing MessageContext properties should not throw
        var act = () =>
        {
            _ = response.MessageId;
            _ = response.CorrelationId;
            _ = response.SentTime;
        };

        act.Should().NotThrow();
    }
}
