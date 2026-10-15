using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class GranularTools
{
    [McpServerTool(Name = "get_order", ReadOnly = true, OpenWorld = false)]
    [Description("Get one order's status, promised delivery date, item quantities and linked payment, reservation and tracking IDs. Follow those IDs with the other tools for payment, warehouse and carrier evidence. Order status may lag carrier updates.")]
    public static string GetOrder([Description("The order ID, for example ORD-1001.")] string orderId)
        => JsonSerializer.Serialize(Domain.GetOrder(orderId), JsonSerializerOptions.Web);

    [McpServerTool(Name = "get_payment", ReadOnly = true, OpenWorld = false)]
    [Description("Get one linked payment's capture status and last update. Dispatch requires a captured payment.")]
    public static string GetPayment([Description("A payment ID returned by get_order.")] string paymentId)
        => JsonSerializer.Serialize(Domain.GetPayment(paymentId), JsonSerializerOptions.Web);

    [McpServerTool(Name = "get_reservation", ReadOnly = true, OpenWorld = false)]
    [Description("Get one linked warehouse reservation: SKU, units, status and last update. Backordered units are not available to dispatch; ready_for_dispatch units are available; dispatched units have left the warehouse.")]
    public static string GetReservation([Description("A reservation ID returned by get_order. An order can have more than one.")] string reservationId)
        => JsonSerializer.Serialize(Domain.GetReservation(reservationId), JsonSerializerOptions.Web);

    [McpServerTool(Name = "get_tracking", ReadOnly = true, OpenWorld = false)]
    [Description("Get one linked shipment's SKU, units, carrier status, latest event, estimate and timestamps. Availability unavailable means carrier evidence could not be retrieved; null status is unknown, not proof of a delay or delivery.")]
    public static string GetTracking([Description("A tracking ID returned by get_order. Read every shipment relevant to the question.")] string trackingId)
        => JsonSerializer.Serialize(Domain.GetTracking(trackingId), JsonSerializerOptions.Web);
}

[McpServerToolType]
public sealed class FocusedTools
{
    [McpServerTool(Name = "get_order_support_context", ReadOnly = true, OpenWorld = false)]
    [Description("Get the operational evidence for one order-support question: order status, promise, item quantities, linked payment, warehouse reservations and carrier tracking with source timestamps. Order status may lag carrier updates. Dispatch requires captured payment; backordered units are not available, ready_for_dispatch units are available, dispatched units have left the warehouse. Tracking availability unavailable means unknown carrier evidence, not proof of a delay or delivery. Returns facts, not an assessment.")]
    public static string GetOrderSupportContext([Description("The order ID, for example ORD-1001.")] string orderId)
        => JsonSerializer.Serialize(Domain.GetContext(orderId), JsonSerializerOptions.Web);
}
