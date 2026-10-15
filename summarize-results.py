"""Validate and summarize one complete live run without making model requests."""

import json
import statistics
import sys
from pathlib import Path


root = Path(__file__).resolve().parent
source = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "results/live-local-01"
manifest = json.loads((source / "manifest.json").read_text())
rows = [json.loads(path.read_text()) for path in sorted(source.glob("*-ORD-*.json"))]
orders = [f"ORD-{number}" for number in range(1001, 1007)]
variants = ["granular", "focused"]
expected = {
    (order, variant, repeat)
    for order in orders
    for variant in variants
    for repeat in range(1, manifest["repetitions"] + 1)
}
actual = [(row["orderId"], row["variant"], row["repeat"]) for row in rows]
if set(actual) != expected or len(actual) != len(expected):
    raise ValueError("The run must contain exactly one artifact for each case, variant and repeat.")

for row in rows:
    if row.get("infrastructureError") or row.get("failure"):
        raise ValueError(f"Incomplete run: {row['orderId']} {row['variant']} repeat {row['repeat']}")
    models = [event for event in row["trace"] if event["kind"] == "model"]
    tools = [event for event in row["trace"] if event["kind"] == "tool"]
    measured = {
        "modelTurns": len(models),
        "toolCalls": len(tools),
        "inputTokens": sum(event["response"]["usage"]["input_tokens"] for event in models),
        "outputTokens": sum(event["response"]["usage"]["output_tokens"] for event in models),
        "cachedInputTokens": sum(
            event["response"]["usage"].get("input_tokens_details", {}).get("cached_tokens", 0)
            for event in models
        ),
        "toolPayloadBytes": sum(len(event["output"].encode("utf-8")) for event in tools),
    }
    if any(row[key] != value for key, value in measured.items()):
        raise ValueError(f"Counters disagree with trace: {row['orderId']} {row['variant']}")
    if row["model"] != manifest["model"] or any(
        event["response"]["model"] != manifest["model"]
        or event["response"]["status"] != "completed"
        for event in models
    ):
        raise ValueError("Unexpected response model or incomplete model response.")
    if row["passed"] != (row["mismatches"] == []):
        raise ValueError("Pass flag disagrees with the recorded scorer output.")
    if any(event["startMs"] + event["durationMs"] > row["elapsedMs"] + 0.1 for event in row["trace"]):
        raise ValueError("Trace event ends after the recorded case duration.")


def stats(values):
    return {"total": sum(values), "median": statistics.median(values), "min": min(values), "max": max(values)}


def summarize(cases):
    metrics = ["toolCalls", "modelTurns", "inputTokens", "outputTokens", "cachedInputTokens", "toolPayloadBytes", "elapsedMs"]
    return {
        "runs": len(cases),
        "structuredMatches": sum(row["passed"] for row in cases),
        **{key: stats([row[key] for row in cases]) for key in metrics},
        "totalTokens": stats([row["inputTokens"] + row["outputTokens"] for row in cases]),
    }


by_variant = {variant: summarize([row for row in rows if row["variant"] == variant]) for variant in variants}
summary = {
    "source": source.name,
    "model": manifest["model"],
    "createdAt": manifest["createdAt"],
    "repetitions": manifest["repetitions"],
    "caseCount": len(orders),
    "scoreDefinition": manifest["scorer"],
    "timing": manifest["timing"],
    "variants": by_variant,
    "medianReductionPercent": {
        key: 100 * (1 - by_variant["focused"][key]["median"] / by_variant["granular"][key]["median"])
        for key in ["totalTokens", "elapsedMs"]
    },
    "cases": [
        {"orderId": order, **{
            variant: summarize([row for row in rows if row["orderId"] == order and row["variant"] == variant])
            for variant in variants
        }}
        for order in orders
    ],
}
destination = source.parent / "live-summary.json"
destination.write_text(json.dumps(summary, indent=2) + "\n")
print(f"Validated {len(rows)} artifacts against their raw traces. Wrote {destination}")
for variant, result in by_variant.items():
    print(f"{variant}: {result['structuredMatches']}/{result['runs']} structured matches; "
          f"median {result['totalTokens']['median']:,.0f} tokens; median {result['elapsedMs']['median'] / 1000:.3f} seconds")
