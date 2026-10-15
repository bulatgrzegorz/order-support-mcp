using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

public static class Checks
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task RunAsync(string? output = null)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = timeout.Token;
        await using var granular = await Server.ConnectAsync("granular", cancellationToken);
        await using var focused = await Server.ConnectAsync("focused", cancellationToken);

        var granularTools = await granular.ListToolsAsync(cancellationToken: cancellationToken);
        var focusedTools = await focused.ListToolsAsync(cancellationToken: cancellationToken);
        Require(granularTools.Select(x => x.Name).Order().SequenceEqual(
            new[] { "get_order", "get_payment", "get_reservation", "get_tracking" }), "Granular tools are discoverable.");
        Require(focusedTools.Select(x => x.Name).SequenceEqual(["get_order_support_context"]), "Focused tool is discoverable.");

        var measurements = new List<Measurement>();
        foreach (var scenario in Domain.Cases)
        {
            var granularPayloads = new List<string>();
            var focusedPayloads = new List<string>();
            var order = await Call<Order>(granular, "get_order", "orderId", scenario.OrderId, cancellationToken, granularPayloads);
            var payment = await Call<Payment>(granular, "get_payment", "paymentId", order.PaymentId, cancellationToken, granularPayloads);
            var reservations = new List<Reservation>();
            foreach (var id in order.ReservationIds)
                reservations.Add(await Call<Reservation>(granular, "get_reservation", "reservationId", id, cancellationToken, granularPayloads));
            var tracking = new List<Tracking>();
            foreach (var id in order.TrackingIds)
                tracking.Add(await Call<Tracking>(granular, "get_tracking", "trackingId", id, cancellationToken, granularPayloads));
            var assembled = new SupportContext(order, payment, [.. reservations], [.. tracking]);
            var context = await Call<SupportContext>(focused, "get_order_support_context", "orderId", scenario.OrderId, cancellationToken, focusedPayloads);

            Require(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(assembled, Json), JsonSerializer.SerializeToNode(context, Json)),
                $"Both transports expose identical facts for {scenario.OrderId}.");
            Require(context.Order.OrderId == scenario.OrderId && context.Payment.OrderId == scenario.OrderId &&
                context.Reservations.All(x => x.OrderId == scenario.OrderId) && context.Tracking.All(x => x.OrderId == scenario.OrderId),
                $"No unrelated order records returned for {scenario.OrderId}.");
            Require(context.Order.Items.Length <= 2 && context.Reservations.Length <= 2 && context.Tracking.Length <= 2,
                "Demo context is bounded to at most two items, reservations and shipments.");
            var json = JsonSerializer.Serialize(context, Json);
            Require(Encoding.UTF8.GetByteCount(json) < 8192, "Demo context fits within 8 KiB.");
            Require(!json.Contains("delayReasons") && !json.Contains("expected") && !json.Contains("scenario"),
                "Evaluator labels are not returned by tools.");
            var measurement = new Measurement(scenario.OrderId, scenario.Scenario,
                granularPayloads.Count, focusedPayloads.Count,
                granularPayloads.Sum(Encoding.UTF8.GetByteCount), focusedPayloads.Sum(Encoding.UTF8.GetByteCount));
            measurements.Add(measurement);
            Console.WriteLine($"PASS {scenario.OrderId}: identical facts; {measurement.GranularCalls} / {measurement.FocusedCalls} calls; " +
                $"{measurement.GranularPayloadBytes} / {measurement.FocusedPayloadBytes} JSON bytes (granular / focused).");
        }

        var unavailable = await Call<SupportContext>(focused, "get_order_support_context", "orderId", "ORD-1005", cancellationToken);
        Require(unavailable.Tracking is [{ Availability: "unavailable", Status: null, LatestEvent: null, EstimatedDeliveryDate: null, UpdatedAt: null }],
            "Unavailable tracking preserves the shipment reference without inventing carrier evidence.");
        Require(unavailable.Tracking[0].CheckedAt == Domain.AsOf, "Unavailable evidence records when retrieval was attempted.");

        var partial = await Call<SupportContext>(focused, "get_order_support_context", "orderId", "ORD-1004", cancellationToken);
        Require(partial.Tracking.Single().Sku == "LAPTOP-14" && partial.Tracking.Single().Status == "delivered" &&
            partial.Reservations.Single(x => x.Sku == "MONITOR-27").Status == "backordered",
            "Partial delivery preserves each SKU's distinct evidence.");
        var stale = await Call<SupportContext>(focused, "get_order_support_context", "orderId", "ORD-1006", cancellationToken);
        Require(stale.Order.Status == "shipped" && stale.Tracking.Single().Status == "delivered" &&
            stale.Order.UpdatedAt < stale.Tracking.Single().UpdatedAt, "Conflicting statuses retain source timestamps.");

        foreach (var invalidId in new[] { "", "ORD-9999", new string('x', 4096), "ORD-1001,ORD-1002" })
        {
            await RequireError(granular, "get_order", "orderId", invalidId, cancellationToken);
            await RequireError(focused, "get_order_support_context", "orderId", invalidId, cancellationToken);
        }
        await RequireError(granular, "get_payment", "paymentId", "PAY-9999", cancellationToken);
        await RequireError(granular, "get_reservation", "reservationId", "RSV-9999-A", cancellationToken);
        await RequireError(granular, "get_tracking", "trackingId", "TRK-9999-A", cancellationToken);
        Console.WriteLine("PASS real MCP stdio discovery, evidence parity, correlation, partial delivery, missing data, timestamps, payload bounds and input errors.");

        if (output is not null)
        {
            var path = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
            {
                datasetAsOf = Domain.AsOf,
                sdk = $"ModelContextProtocol {typeof(McpClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0]}",
                method = "Deterministic client retrieves every linked record over MCP stdio; no LLM. Bytes are UTF-8 tool JSON text only, excluding protocol envelopes, schemas and requests.",
                results = measurements
            }, new JsonSerializerOptions(Json) { WriteIndented = true }), cancellationToken);
            Console.WriteLine($"Wrote {path}");
        }
    }

    private static async Task<T> Call<T>(McpClient client, string tool, string parameter, string value,
        CancellationToken cancellationToken, List<string>? payloads = null)
    {
        var result = await client.CallToolAsync(tool, new Dictionary<string, object?> { [parameter] = value }, cancellationToken: cancellationToken);
        Require(result.IsError is not true, $"{tool} succeeds.");
        var json = Server.ResultJson(result);
        payloads?.Add(json);
        return JsonSerializer.Deserialize<T>(json, Json) ?? throw new InvalidOperationException("Empty tool response.");
    }

    private static async Task RequireError(McpClient client, string tool, string parameter, string value, CancellationToken cancellationToken)
    {
        var result = await client.CallToolAsync(tool, new Dictionary<string, object?> { [parameter] = value }, cancellationToken: cancellationToken);
        Require(result.IsError is true && result.Content.OfType<TextContentBlock>().Any(x => !string.IsNullOrWhiteSpace(x.Text)),
            $"{tool} reports an actionable error for an invalid or unknown ID.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private record Measurement(string OrderId, string Scenario, int GranularCalls, int FocusedCalls,
        int GranularPayloadBytes, int FocusedPayloadBytes);
}
