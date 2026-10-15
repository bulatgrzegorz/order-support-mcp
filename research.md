# Order support through MCP

Research and implementation notes, 2026-10-08. The user approved the first proposed topic: compare granular C# MCP tools with a tool that assembles the evidence needed for an order-support question.

## Technical basis

- The official C# SDK supports attribute-based tool discovery, standard .NET hosting and stdio transport. Console logs must use stderr so stdout remains the protocol channel. Microsoft's original [C# server walkthrough](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/) documents those conventions and JSON text tool results.
- Tool design is an experimental variable: meaningful responses, clear descriptions and task-relevant operations can change agent behavior. Anthropic recommends testing tools against verifiable tasks and tracking usage as well as success. This is motivation for our comparison, not evidence of our own result. [Writing effective tools for agents](https://www.anthropic.com/engineering/writing-tools-for-agents).
- An MCP client can discover the tool definitions and invoke them. Our evaluator maps those definitions into model function tools, then sends each selected call through the SDK client. The model endpoint never connects directly to the local stdio process. [Official SDK repository](https://github.com/modelcontextprotocol/csharp-sdk), [OpenAI function calling](https://developers.openai.com/api/docs/guides/function-calling).
- The Responses API accepts `function_call_output` items and a JSON-schema response format under `text.format`. We use the former for operational evidence and the latter for the final assessment. The final schema is a grading convenience, not proof of factual accuracy. [Structured outputs](https://developers.openai.com/api/docs/guides/structured-outputs).
- The official model page lists `gpt-4.1-mini-2025-04-14` as a pinned snapshot supporting function calling and structured outputs. Account access and working network connectivity remain prerequisites for executing the optional evaluation. [Model reference](https://developers.openai.com/api/docs/models/gpt-4.1-mini).

## Reproduction environment and limits

The machine has .NET SDK 10.0.103 / runtime 10.0.3. The example now pins `ModelContextProtocol` **2.2.0** and `Microsoft.Extensions.Hosting` **10.0.4**, with a NuGet lock file. Tools deliberately return serialized JSON strings as MCP text content, keeping the response format and payload measurements consistent across both designs. Structured content is available in the SDK but is outside this comparison.

The initial implementation used the cached `ModelContextProtocol` **0.1.0-preview.6** because requests to `api.nuget.org` failed. The package has since been upgraded. Earlier live attempts, [live-attempt-02](results/live-attempt-02/) and [live-mcp-2.2.0-01](results/live-mcp-2.2.0-01/), failed before completing a model turn. Connectivity checks could not resolve `api.openai.com`, including a retry outside the sandbox. Both attempts are preserved as historical infrastructure failures and excluded from model-result aggregates.

With 2.2.0, the build completed without warnings or errors, all six real stdio scenarios and error checks passed, and the evaluator's scoring and scripted HTTP-to-MCP self-checks passed. The regenerated deterministic results record the loaded SDK version; calls and payload bytes are unchanged.

Rebuild and repeat the transport checks after package upgrades before recording new measurements.

The user subsequently completed [live-local-01](results/live-local-01/) on October 8, 2026, with its manifest created at 13:44:49 UTC. All 36 case artifacts completed successfully. Both designs matched the five scored fixture fields in 18/18 cases; the median total token counts were 3,361 granular and 1,953 focused, and median model-loop times were 4,044.245 ms and 2,691.905 ms. The [generated summary](results/live-summary.json) and [summary script](summarize-results.py) make those calculations reproducible from the preserved traces.

The live manifest records model and evaluation settings but no build or SDK identity. The current project and deterministic check use MCP 2.2.0; the live artifacts alone do not establish the exact compiled SDK version used for that run.

## Experiment contract

Six synthetic orders share the same fixed time and account scope. Every tool is read-only. The granular surface has `get_order`, `get_payment`, `get_reservation` and `get_tracking`. The focused surface has `get_order_support_context`. The focused implementation only gathers the records accessible through the granular surface; it supplies no cause classification or prewritten answer.

Two different measurements are deliberately kept distinct:

1. **Deterministic MCP check:** start both actual stdio servers, discover and invoke tools, fetch every linked record, compare complete evidence, count calls and measure UTF-8 JSON text payload bytes. This is an exhaustive retrieval script, not an LLM choosing tools. Bytes exclude JSON-RPC framing and are not token counts. All six rows must pass before the result artifact is written.
2. **Optional model evaluation:** fresh conversation per variant and case, identical prompt and answer schema, pinned model, three repeats, alternating variant order per pair. Allow parallel tool requests and a common generous budget (32 calls / 12 model turns). Record returned usage, tool calls, model turns, elapsed time, responses and tool traces. Exact fixture fields are scored; customer-facing prose is saved but not scored.

The evaluator asks neutrally what is happening with the order; it does not tell the model that every order is late. The shared instructions define status meanings and which timestamped source governs delivery. Missing carrier data is explicit, not an empty successful lookup. Expected assessments are held outside the exposed tools.

No current dataset tests production authorization, live backend performance, large orders, changing data, failures beyond the modeled tracking outage, or broad generalization across customer questions. The focused tool eagerly gathers all linked evidence; granular tools could make fewer backend queries for a narrower question. A reduced number of MCP calls alone proves neither lower tokens nor lower latency nor greater answer accuracy.

The successful live run measures provider-reported tokens and elapsed time separately from the deterministic retrieval check. Its timing excludes MCP startup and discovery and uses in-memory order backends. Six handpicked cases with three repetitions are illustrative, not a reliability study. The scorer checks five structured fields and does not grade the customer-facing summary: for example, the first two focused `ORD-1005` summaries refer to a delay even though the carrier state is unknown. All passed fixture checks therefore do not establish perfect customer replies.

## Editorial direction

Open with an office-equipment order whose laptop arrived and monitor did not. Show the record-by-record tool surface, move the fixed joins into C#, preserve uncertainty and timestamps, then show the checks and measured results. Use the completed live run to discuss token usage, elapsed time and an actual trace, while distinguishing scored fields from prose quality. Use inline Mermaid diagrams; preserve Cloudinary front matter with an existing cover URL while the draft is under review.
