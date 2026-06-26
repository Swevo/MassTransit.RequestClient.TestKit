// ── Test message contracts ────────────────────────────────────────────────────

public record GetDevice(int DeviceId);
public record DeviceFound(int DeviceId, string Name);
public record DeviceNotFound(int DeviceId, string Reason);
public record DeviceError(string Message);
public record PlaceOrder(Guid OrderId, decimal Amount);
public record OrderAccepted(Guid OrderId);
public record OrderRejected(Guid OrderId, string Reason);
