# Recorded evidence

## Deterministic retrieval

[deterministic-comparison.json](deterministic-comparison.json) contains successful real MCP stdio retrieval checks. Each granular run reads the order and every linked payment, reservation and tracking record; each focused run reads the same evidence in one response. No LLM makes decisions in this measurement.

| Quantity | Granular | Focused |
| --- | ---: | ---: |
| Total calls across six orders | 23 | 6 |
| Total UTF-8 JSON text bytes | 4,271 | 4,590 |

The byte counts exclude protocol envelopes, schemas and conversation history. Focused output is larger because it adds the `order`, `payment`, `reservations` and `tracking` envelope. The measurements do not establish model latency, token consumption or accuracy.

## Completed live evaluation

The user completed [live-local-01](live-local-01/) on October 8, 2026. Its [manifest](live-local-01/manifest.json) was created at 13:44:49 UTC and records `gpt-4.1-mini-2025-04-14`, three repetitions, shared instructions, the assessment schema and budgets. The run contains 36 completed cases: six orders × two designs × three repetitions. Each artifact preserves the answer, usage, timings and observable model/tool trace; [tools.json](live-local-01/tools.json) preserves the discovered definitions.

The manifest does not record build or SDK identity. The current project and deterministic check use MCP 2.2.0, but the live artifacts alone do not establish the compiled SDK version used for this run.

| Quantity | Granular | Focused |
| --- | ---: | ---: |
| Cases matching all five scored fields | 18 / 18 | 18 / 18 |
| Total tool calls | 69 | 18 |
| Total model turns | 54 | 36 |
| Total input tokens | 56,310 | 32,481 |
| Total output tokens | 3,678 | 2,198 |
| Reported cached input tokens | 0 | 0 |
| Median input + output tokens per case | 3,361 | 1,953 |
| Median elapsed time per case | 4,044.245 ms | 2,691.905 ms |

Medians are calculated across the 18 cases for each design. Total tokens per case sum provider-reported input and output usage over every model turn, including repeated context. Elapsed time starts just before the first model request and includes API calls, local MCP calls and serialization; it excludes MCP process startup and tool discovery. Order, payment, warehouse and carrier records come from in-memory fixtures, so these timings do not measure remote backend performance.

The [summary script](../summarize-results.py) derives [live-summary.json](live-summary.json) from this run. Visuals are available as the [comparison SVG](../figures/live-comparison.svg) / [PNG](../figures/live-comparison.png) and [partial-delivery trace SVG](../figures/live-order-1004.svg) / [PNG](../figures/live-order-1004.png). The trace uses recorded events from `ORD-1004`; the underlying per-case JSON remains the evidence.

From the repository root, validate all 36 traces and regenerate the summary followed by all three figures:

```console
python3 _planning/order-support-mcp/summarize-results.py
node _planning/order-support-mcp/render-figures.mjs
```

The Python script defaults to `results/live-local-01`, accepts an optional source-directory argument and writes `results/live-summary.json`. The renderer uses D3 and Sharp from the existing Astro preview. These commands make no model requests.

**Passing the scorer is narrower than a correct customer reply.** The scorer checks delivered SKUs, outstanding SKUs, delay reasons, the delivery estimate and missing evidence against the fixture. It does not grade the prose summary. For example, the [first focused unavailable-tracking reply](live-local-01/1-ORD-1005-focused.json) refers to a delay even though tracking is unavailable and delivery is unconfirmed; the second repetition does the same. These replies pass the five field checks. Six handpicked cases with three repetitions illustrate the tradeoff and do not establish general reliability or perfect answer quality.

Both designs expose the same operational evidence. The observed token and elapsed-time differences belong to this live run; they cannot be inferred from the deterministic call and byte counts above.

## Historical failed attempts

[live-attempt-02](live-attempt-02/) and [live-mcp-2.2.0-01](live-mcp-2.2.0-01/) record earlier aborted live evaluations and are excluded from every aggregate above. MCP startup and discovery succeeded, but the first API request failed with `HttpRequestException` before any model turn completed. Direct DNS checks also failed for `api.openai.com`, including outside the sandbox. The manifests and discovered tool definitions are preserved to make the attempted experiments inspectable. Zero tokens/calls there mean no completed response or selected tool call was observed; they are not model performance results. Their `passed: false` values are not a model error rate.
