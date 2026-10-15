using ModelContextProtocol;

public record OrderItem(string Sku, int Quantity);
public record Order(
    string OrderId, string Status, DateOnly PromisedDeliveryDate, DateTimeOffset UpdatedAt,
    OrderItem[] Items, string PaymentId, string[] ReservationIds, string[] TrackingIds);
public record Payment(string PaymentId, string OrderId, string Status, DateTimeOffset UpdatedAt);
public record Reservation(
    string ReservationId, string OrderId, string Sku, int Quantity, string Status, DateTimeOffset UpdatedAt);
public record Tracking(
    string TrackingId, string OrderId, string Sku, int Quantity, string Availability,
    string? Status, string? LatestEvent, DateOnly? EstimatedDeliveryDate,
    DateTimeOffset? UpdatedAt, DateTimeOffset CheckedAt);
public record SupportContext(Order Order, Payment Payment, Reservation[] Reservations, Tracking[] Tracking);

public record ExpectedAssessment(
    string[] DeliveredSkus, string[] OutstandingSkus, string[] DelayReasons,
    string? EstimatedDeliveryDate, string[] MissingEvidence);
public record SupportCase(string OrderId, string Scenario, ExpectedAssessment Expected);

public static class Domain
{
    public static readonly DateTimeOffset AsOf = DateTimeOffset.Parse("2026-10-08T12:00:00Z");

    // These labels belong to the evaluator. Tools only read the operational records below.
    public static readonly SupportCase[] Cases =
    [
        new("ORD-1001", "Payment awaiting capture", new([], ["MONITOR-27"], ["payment_pending"], null, [])),
        new("ORD-1002", "Stock shortage", new([], ["DOCK-USB-C"], ["stock_shortage"], null, [])),
        new("ORD-1003", "Carrier delay with a revised estimate", new([], ["KEYBOARD-US"], ["carrier_delay"], "2026-10-10", [])),
        new("ORD-1004", "Partial delivery with a backordered item", new(["LAPTOP-14"], ["MONITOR-27"], ["stock_shortage"], null, [])),
        new("ORD-1005", "Tracking provider unavailable", new([], ["HEADSET-USB"], ["unknown"], null, ["tracking:TRK-1005-A"])),
        new("ORD-1006", "Stale order status after confirmed delivery", new(["WEBCAM-HD"], [], [], null, []))
    ];

    private static readonly Order[] Orders =
    [
        new("ORD-1001", "processing", new(2026, 10, 7), At("2026-10-06T08:00:00Z"), [new("MONITOR-27", 2)], "PAY-1001", ["RSV-1001-A"], []),
        new("ORD-1002", "processing", new(2026, 10, 7), At("2026-10-07T09:00:00Z"), [new("DOCK-USB-C", 1)], "PAY-1002", ["RSV-1002-A"], []),
        new("ORD-1003", "shipped", new(2026, 10, 7), At("2026-10-06T15:00:00Z"), [new("KEYBOARD-US", 3)], "PAY-1003", ["RSV-1003-A"], ["TRK-1003-A"]),
        new("ORD-1004", "partially_shipped", new(2026, 10, 7), At("2026-10-06T15:00:00Z"), [new("LAPTOP-14", 1), new("MONITOR-27", 1)], "PAY-1004", ["RSV-1004-A", "RSV-1004-B"], ["TRK-1004-A"]),
        new("ORD-1005", "shipped", new(2026, 10, 7), At("2026-10-06T16:00:00Z"), [new("HEADSET-USB", 2)], "PAY-1005", ["RSV-1005-A"], ["TRK-1005-A"]),
        new("ORD-1006", "shipped", new(2026, 10, 7), At("2026-10-06T16:00:00Z"), [new("WEBCAM-HD", 1)], "PAY-1006", ["RSV-1006-A"], ["TRK-1006-A"])
    ];

    private static readonly Payment[] Payments =
    [
        new("PAY-1001", "ORD-1001", "pending_capture", At("2026-10-08T11:55:00Z")),
        new("PAY-1002", "ORD-1002", "captured", At("2026-10-05T08:00:00Z")),
        new("PAY-1003", "ORD-1003", "captured", At("2026-10-05T08:00:00Z")),
        new("PAY-1004", "ORD-1004", "captured", At("2026-10-05T08:00:00Z")),
        new("PAY-1005", "ORD-1005", "captured", At("2026-10-05T08:00:00Z")),
        new("PAY-1006", "ORD-1006", "captured", At("2026-10-05T08:00:00Z"))
    ];

    private static readonly Reservation[] Reservations =
    [
        new("RSV-1001-A", "ORD-1001", "MONITOR-27", 2, "ready_for_dispatch", At("2026-10-08T11:50:00Z")),
        new("RSV-1002-A", "ORD-1002", "DOCK-USB-C", 1, "backordered", At("2026-10-08T11:50:00Z")),
        new("RSV-1003-A", "ORD-1003", "KEYBOARD-US", 3, "dispatched", At("2026-10-06T15:00:00Z")),
        new("RSV-1004-A", "ORD-1004", "LAPTOP-14", 1, "dispatched", At("2026-10-06T15:00:00Z")),
        new("RSV-1004-B", "ORD-1004", "MONITOR-27", 1, "backordered", At("2026-10-08T11:50:00Z")),
        new("RSV-1005-A", "ORD-1005", "HEADSET-USB", 2, "dispatched", At("2026-10-06T16:00:00Z")),
        new("RSV-1006-A", "ORD-1006", "WEBCAM-HD", 1, "dispatched", At("2026-10-06T16:00:00Z"))
    ];

    private static readonly Tracking[] TrackingRecords =
    [
        new("TRK-1003-A", "ORD-1003", "KEYBOARD-US", 3, "available", "delayed", "Weather disruption at regional sorting hub", new(2026, 10, 10), At("2026-10-08T10:30:00Z"), AsOf),
        new("TRK-1004-A", "ORD-1004", "LAPTOP-14", 1, "available", "delivered", "Delivered to the receiving desk", null, At("2026-10-07T13:20:00Z"), AsOf),
        new("TRK-1005-A", "ORD-1005", "HEADSET-USB", 2, "unavailable", null, null, null, null, AsOf),
        new("TRK-1006-A", "ORD-1006", "WEBCAM-HD", 1, "available", "delivered", "Delivered to the receiving desk", null, At("2026-10-07T14:05:00Z"), AsOf)
    ];

    public static Order GetOrder(string orderId) => Find(Orders, x => x.OrderId, orderId, "ORD-", 8);
    public static Payment GetPayment(string paymentId) => Find(Payments, x => x.PaymentId, paymentId, "PAY-", 8);
    public static Reservation GetReservation(string reservationId) => Find(Reservations, x => x.ReservationId, reservationId, "RSV-", 10);
    public static Tracking GetTracking(string trackingId) => Find(TrackingRecords, x => x.TrackingId, trackingId, "TRK-", 10);

    public static SupportContext GetContext(string orderId)
    {
        var order = GetOrder(orderId);
        return new SupportContext(
            order, 
            GetPayment(order.PaymentId),
            order.ReservationIds.Select(GetReservation).ToArray(),
            order.TrackingIds.Select(GetTracking).ToArray());
    }

    private static T Find<T>(T[] records, Func<T, string> key, string id, string prefix, int length) where T : class
    {
        if (id is null || id.Length != length || !id.StartsWith(prefix, StringComparison.Ordinal)) throw new McpException($"Expected one {prefix} identifier with {length} characters.");
        return records.FirstOrDefault(record => key(record) == id) ?? throw new McpException($"No record for {id} in this demo dataset.");
    }

    private static DateTimeOffset At(string value) => DateTimeOffset.Parse(value);
}
