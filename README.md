# Swevo.MassTransit.TestKit

[![NuGet](https://img.shields.io/nuget/v/Swevo.MassTransit.TestKit
[![NuGet Downloads](https://img.shields.io/nuget/dt/Swevo.MassTransit.TestKit.svg)](https://www.nuget.org/packages/Swevo.MassTransit.TestKit).svg)](https://www.nuget.org/packages/Swevo.MassTransit.TestKit/)
[![Build](https://github.com/Swevo/Swevo.MassTransit.TestKit/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/Swevo.MassTransit.TestKit/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Lightweight test doubles for MassTransit request/response patterns. Drop-in fakes for `IRequestClient<T>` and `IPublishEndpoint` — no harness, no bus, no infrastructure.

---

## Why?

`MassTransit.Testing`'s `InMemoryTestHarness` is great for integration tests, but unit tests shouldn't need a bus. This package gives you:

| Type | Replaces |
|---|---|
| `FakeRequestClient<TRequest>` | `IRequestClient<TRequest>` |
| `FakePublishEndpoint` | `IPublishEndpoint` |

---

## Installation

```bash
dotnet add package Swevo.MassTransit.TestKit
```

---

## Quick Start

### FakeRequestClient — Single Response

```csharp
var client = new FakeRequestClient<GetDevice>()
    .RespondWith(new DeviceFound(42, "Thermostat"));

var response = await client.GetResponse<DeviceFound>(new GetDevice(42));

response.Message.DeviceId.Should().Be(42);
client.WasCalled.Should().BeTrue();
```

### FakeRequestClient — Union Response

```csharp
var client = new FakeRequestClient<PlaceOrder>()
    .RespondWith(new OrderAccepted(orderId));

var (accepted, rejected) = await client.GetResponse<OrderAccepted, OrderRejected>(
    new PlaceOrder(orderId, 99.99m));

(await accepted).Message.OrderId.Should().Be(orderId);
rejected.IsCompleted.Should().BeFalse(); // non-matching branch never completes
```

### FakeRequestClient — Exception

```csharp
var client = new FakeRequestClient<GetDevice>()
    .Throws(new TimeoutException());

Func<Task> act = () => client.GetResponse<DeviceFound>(new GetDevice(42));
await act.Should().ThrowAsync<TimeoutException>();
```

### FakePublishEndpoint

```csharp
var publish = new FakePublishEndpoint();

await publish.Publish(new DeviceRegistered(42));

publish.WasPublished<DeviceRegistered>().Should().BeTrue();
publish.MostRecent<DeviceRegistered>()!.DeviceId.Should().Be(42);
```

---

## API Reference

### `FakeRequestClient<TRequest>`

| Member | Description |
|---|---|
| `.RespondWith(message)` | Configure the response message (fluent) |
| `.Throws(exception)` | Configure a thrown exception (fluent) |
| `WasCalled` | `true` if any request was sent |
| `CallCount` | Number of requests sent |
| `MostRecentRequest` | Last request message sent |
| `ReceivedRequests` | All requests in order |

### `FakePublishEndpoint`

| Member | Description |
|---|---|
| `GetMessages<T>()` | All published messages of type `T` |
| `WasPublished<T>()` | `true` if any message of type `T` was published |
| `MostRecent<T>()` | Most recently published message of type `T` |
| `Clear()` | Reset all captured messages |

---

## Usage in SCM

The SCM codebase uses `testHarness.Value.GetRequestClient<T>()` in `WebApiClientDriver`. For **unit tests** that don't need the full harness, swap in a fake:

```csharp
// Instead of starting the web host:
var client = new FakeRequestClient<GetDevice>()
    .RespondWith(new DeviceFound(deviceId, "Thermostat"));

var handler = new GetDeviceQueryHandler(client);
var result = await handler.Handle(new GetDeviceQuery(deviceId), CancellationToken.None);
```

---

## Compatibility

| Package | Version |
|---|---|
| MassTransit | 9.x |
| .NET | net8.0+ |

---

## License

MIT © 2025 Justin Bannister
