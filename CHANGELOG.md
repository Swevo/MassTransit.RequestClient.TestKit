# Changelog

## [1.0.0] - 2025-01-01

### Added
- `FakeRequestClient<TRequest>` — test double for `IRequestClient<TRequest>`
  - `RespondWith(message)` and `Throws(exception)` fluent configuration
  - Single-response: `GetResponse<T>(...)`
  - Two-way union: `GetResponse<T1,T2>(...)` — matching branch completes, other stays pending
  - Three-way union: `GetResponse<T1,T2,T3>(...)` 
  - Request capture: `WasCalled`, `CallCount`, `MostRecentRequest`, `ReceivedRequests`
- `FakePublishEndpoint` — test double for `IPublishEndpoint`
  - Captures published messages in a `ConcurrentQueue`
  - `GetMessages<T>()`, `WasPublished<T>()`, `MostRecent<T>()`, `Clear()`
