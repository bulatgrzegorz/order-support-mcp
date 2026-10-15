# One support question, too many tool calls

The [blog draft](../../_wip/2026-10-08-one-support-question-too-many-tool-calls.md) follows an office-equipment support request through two C# MCP interfaces: individual record lookups and one tool assembling the same evidence.

- [Runnable C# example and commands](demo/README.md)
- [Primary sources and experiment design](research.md)
- [Recorded measurements and their limits](results/README.md)
- [Retrieval chart, SVG](figures/retrieval-calls.svg) / [PNG](figures/retrieval-calls.png)
- [Live comparison, SVG](figures/live-comparison.svg) / [PNG](figures/live-comparison.png)
- [Partial-delivery trace, SVG](figures/live-order-1004.svg) / [PNG](figures/live-order-1004.png)
- [Live run artifacts](results/live-local-01/) and [generated summary](results/live-summary.json)

## Verified

- The .NET project builds with zero warnings and errors using its locked dependencies, including `ModelContextProtocol` 2.2.0. The transport and evaluator self-checks below were repeated after the upgrade.
- Both stdio MCP servers start, expose the intended tools and return identical records for all six orders.
- Checks cover partial delivery, missing carrier data, conflicting timestamps, invalid IDs, unrelated-order exclusion and fixture payload size.
- The deterministic retrieval comparison records 23 granular calls versus 6 focused calls; JSON text totals are 4,271 versus 4,590 bytes.
- The evaluator's scorer rejects missing, extra, duplicated and malformed facts and invented delivery dates. A scripted HTTP model response exercises a real MCP call, its result being returned to the model adapter, and final assessment scoring. This check makes no live model request.
- The user completed a live run on October 8, 2026: six scenarios × two designs × three repetitions, using `gpt-4.1-mini-2025-04-14`. Both designs matched all five scored assessment fields in 18/18 cases. Median input-plus-output tokens were 3,361 granular versus 1,953 focused; median model-loop elapsed time was 4.04 versus 2.69 seconds. These measurements come from the live artifacts, separately from the deterministic retrieval check.
- All three D3 figures are generated from saved measurements and exported as SVG and PNG; the PNGs were visually inspected.
- The front matter and code fences are checked separately from the site's build. The article includes four inline Mermaid diagrams. Browser rendering was attempted but Chrome could not start in this environment; a Node-only Mermaid parser accepted the two sequence diagrams, while flowchart parsing required a browser DOM.

## Before publication

1. Upload the chosen cover and charts to Cloudinary, use their real URLs, and replace the companion source link after publishing its repository location. The draft currently reuses an existing Cloudinary cover and includes the measurements as tables.
2. Render the post in the site's preview to inspect the Mermaid diagrams with a working browser.

The live scorer does not grade customer-facing prose. Some unavailable-tracking summaries imply a confirmed delay despite the missing evidence. Six handpicked cases with three repetitions illustrate this comparison; they do not establish general reliability. Timing excludes MCP startup and discovery, and all order data comes from in-memory backends. See [the result notes](results/README.md) for the complete limits and preserved traces.

To validate all 36 saved traces, regenerate [the summary](results/live-summary.json), then render all three figures without making model requests, run from the repository root:

```console
python3 _planning/order-support-mcp/summarize-results.py
node _planning/order-support-mcp/render-figures.mjs
```

The [summary script](summarize-results.py) defaults to `results/live-local-01` and accepts an optional source-directory argument. The renderer uses D3 and Sharp already installed in the repository's Astro preview.

No site configuration, published post or existing working-tree change was modified.
