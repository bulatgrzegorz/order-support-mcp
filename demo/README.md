# One support question, two MCP tool designs

This .NET 10 executable serves the same six synthetic office-equipment orders through two MCP interfaces:

- `granular`: `get_order`, `get_payment`, `get_reservation`, `get_tracking`.
- `focused`: `get_order_support_context`, which gathers exactly the same records in one response.

The focused tool does not generate an answer or return the expected assessment. All lookups use one fixed demo account and a frozen timestamp: `2026-10-08T12:00:00Z`. The data includes a pending payment, a stock shortage, a carrier delay, a partial delivery, unavailable tracking, and delivery confirmed after the order status became stale.

## Build and verify

From the blog repository root:

```console
dotnet restore _planning/order-support-mcp/demo/OrderSupport.csproj --locked-mode
dotnet build _planning/order-support-mcp/demo/OrderSupport.csproj --no-restore
```

The example targets .NET 10 and pins `ModelContextProtocol` **2.2.0** and `Microsoft.Extensions.Hosting` **10.0.4**, with a NuGet lock file. The reproduction environment has .NET SDK **10.0.103** and runtime **10.0.3**. Tools deliberately return serialized JSON strings as MCP text content so both designs use the same response format and payload measurement. The SDK also supports structured content.

For an offline build on a machine with all locked packages already cached, point restore at that machine's NuGet package cache and disable the network vulnerability lookup for that invocation. This is an environment workaround, not a project-wide restore setting:

```console
dotnet restore _planning/order-support-mcp/demo/OrderSupport.csproj --locked-mode --source /path/to/your/.nuget/packages -p:NuGetAudit=false
```

Recorded payload bytes are UTF-8 bytes of the JSON **inside the tool's text content**, summed across the exhaustive retrieval. They exclude MCP envelopes, tool definitions, model prompts and responses. They are not model tokens. The script deliberately follows every link; it does not measure how many tools a model would choose to call.

## Connect a client

After building, configure a stdio client to launch:

```console
dotnet /absolute/path/to/demo/bin/Debug/net10.0/OrderSupport.dll serve granular
dotnet /absolute/path/to/demo/bin/Debug/net10.0/OrderSupport.dll serve focused
```

Choose one command at a time. Stdout carries MCP messages; host logging goes to stderr. Sample question: “What is happening with order ORD-1004? What has arrived, what is still outstanding, and when will the rest arrive?”

## Optional live model evaluation

The live runner requires network access to the OpenAI API and an `OPENAI_API_KEY` supplied through the environment. It sends only the synthetic fixture data, tool descriptions and prompts. It makes paid API requests. A default run performs 18 cases (6 orders × 3 repeats) with the granular tools, each bounded by 12 model turns, 32 tool calls and a per-response output cap.

```console
dotnet run --project _planning/order-support-mcp/demo --no-build -- evaluate
```

Optional positional arguments set the tool design (`granular` default, or `focused`), repetitions, model and provider (`openai` or `anthropic`), for example a single focused repetition of the default pinned snapshot:

```console
dotnet run --project _planning/order-support-mcp/demo --no-build -- evaluate focused 1 gpt-4.1-mini-2025-04-14
```

Each run writes to a new timestamped folder, `results/live-<tools>-<yyyyMMdd-HHmmss>`, with one JSON artifact per case (pass/fail, mismatched fields, failure, tool calls, turns, elapsed time, answer). The console logs one colored line per case and a pass total. Tokens from failed requests that return no usage cannot be accounted for. Credentials and HTTP request headers are never written to artifacts.

The adapter discovers MCP tools and exposes their schemas as model function tools. Function calls execute through the local MCP client. Calls selected together may run in parallel; subsequent model requests include the prior transcript. Both designs get identical domain instructions, a fresh conversation and the same output schema. Pair order alternates by case and repetition. No application response cache is used; any provider-reported cached input tokens are recorded separately.

### Reading the results

- `passed`: all five structured assessment fields exactly match the hand-authored fixture, ignoring array order but rejecting duplicates. It does **not** grade the prose summary.
- `toolCalls` and `modelTurns`: different counters; several tools can be selected in one turn.
- `inputTokens` / `outputTokens`: summed provider-reported usage across all model requests. Repeated context is counted each time it is sent. `cachedInputTokens` is a subset of input, not extra tokens.
- `elapsedMs`: measured from just before the first model request until the case finishes. Excludes MCP process startup and discovery; includes API requests and local tool calls. There are no actual remote payment, warehouse or carrier services in this fixture.
- `toolPayloadBytes`: bytes in the tool output passed to the model, including the content/error wrapper. This differs intentionally from the raw JSON text byte count in the deterministic check.
- `trace`: observable tool calls/results and model responses, with timing. Does not request hidden chain-of-thought.

Three repeats of six handcrafted scenarios are an illustrative evaluation, not a statistically convincing reliability study. Review the prose and individual failures before making conclusions. Do not treat fewer tool calls as proof of fewer backend queries: the focused implementation always loads every linked record.

## Recorded live run and limits

The user completed [live-local-01](../results/live-local-01/) on October 8, 2026, using `gpt-4.1-mini-2025-04-14` and three repetitions per scenario and design. Its manifest was created at 13:44:49 UTC. All 36 case artifacts completed; both designs matched all five scored fields in 18/18 cases. Median input-plus-output tokens were 3,361 granular versus 1,953 focused, and median elapsed time was 4.04 versus 2.69 seconds. See the [aggregate results](../results/live-summary.json), [live comparison](../figures/live-comparison.svg) and [partial-delivery trace](../figures/live-order-1004.svg).

To validate all 36 traces and regenerate the summary and three figures from the repository root:

```console
python3 _planning/order-support-mcp/summarize-results.py
node _planning/order-support-mcp/render-figures.mjs
```

The [summary script](../summarize-results.py) defaults to `results/live-local-01` and accepts an optional source-directory argument. The renderer uses the existing Astro preview's D3 and Sharp dependencies. Neither command makes model requests.

The scorer does not grade customer-facing prose. Some summaries for unavailable tracking imply a confirmed delay despite missing carrier evidence, so matching the fixture fields is not proof of a fully correct reply. These six handpicked scenarios and three repetitions are an illustrative comparison, with in-memory order backends and timing that excludes MCP startup and discovery. The earlier failed attempts, [live-attempt-02](../results/live-attempt-02/) and [live-mcp-2.2.0-01](../results/live-mcp-2.2.0-01/), are retained separately and excluded from the live aggregates. The successful run's manifest does not record build or SDK identity; its artifacts alone do not establish the compiled SDK version, while the current project and deterministic check use MCP 2.2.0.

The local MCP and scoring checks also run without a model. Model token and elapsed-time results come from the live run, not the deterministic retrieval artifact. See [research and methodology](../research.md) for the source references and [results](../results/README.md) for metrics, measurement limits and preserved traces.
