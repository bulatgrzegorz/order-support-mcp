// Uses plotting/image-export dependencies already installed in this repository's Astro preview.
import fs from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { scaleBand, scaleLinear } from '../bulatgrzegorz.github.io/_preview/astro-paper/node_modules/d3/src/index.js';
// import { scaleBand, scaleLinear } from 'index-d3.js';
import sharp from '../bulatgrzegorz.github.io/_preview/astro-paper/node_modules/sharp/dist/index.mjs';
// import sharp from 'index.mjs';

const folder = new URL('./figures/', import.meta.url);
await fs.mkdir(folder, { recursive: true });
const rows = JSON.parse(await fs.readFile(new URL('./results/deterministic-comparison.json', import.meta.url), 'utf8')).results;
const x = scaleBand().domain(rows.map(row => row.orderId)).range([90, 1030]).padding(0.3);
const y = scaleLinear().domain([0, 5]).range([400, 140]);
const colors = ['#256b83', '#c57a42'];
const xml = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
let content = '';
for (const tick of y.ticks(5)) {
  content += `<line x1="90" x2="1030" y1="${y(tick)}" y2="${y(tick)}" stroke="#dae3e7"/>`;
  content += `<text x="72" y="${y(tick) + 5}" text-anchor="end" font-size="16">${tick}</text>`;
}
for (const row of rows) {
  for (const [index, field] of ['granularCalls', 'focusedCalls'].entries()) {
    const barX = x(row.orderId) + index * x.bandwidth() / 2;
    const width = x.bandwidth() / 2 - 6;
    content += `<rect x="${barX}" y="${y(row[field])}" width="${width}" height="${400 - y(row[field])}" rx="4" fill="${colors[index]}"/>`;
    content += `<text x="${barX + width / 2}" y="${y(row[field]) - 12}" text-anchor="middle" font-size="19" font-weight="700">${row[field]}</text>`;
  }
  content += `<text x="${x(row.orderId) + x.bandwidth() / 2}" y="431" text-anchor="middle" font-size="16">${xml(row.orderId)}</text>`;
}
for (const [index, label] of ['Granular tools', 'Focused tool'].entries()) {
  content += `<rect x="${340 + index * 230}" y="463" width="19" height="19" rx="3" fill="${colors[index]}"/>`;
  content += `<text x="${371 + index * 230}" y="479" font-size="18">${label}</text>`;
}
const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1100 570" role="img" aria-labelledby="title desc">
<title id="title">Same evidence, fewer MCP retrieval calls</title>
<desc id="desc">Six synthetic orders. Granular exhaustive retrieval uses 3, 3, 4, 5, 4 and 4 calls. Focused retrieval uses one per order. No LLM is involved.</desc>
<rect width="1100" height="570" rx="18" fill="#fafbf9"/>
<g font-family="Arial, Helvetica, sans-serif" fill="#18313e">
<text x="44" y="51" font-size="30" font-weight="700">Same evidence, fewer MCP retrieval calls</text>
<text x="44" y="83" font-size="17" fill="#506974">Exhaustive scripted retrieval · six synthetic orders · no LLM</text>
<text x="44" y="117" font-size="13" font-weight="700" fill="#506974">MCP CALLS</text>
${content}
<line x1="44" x2="1056" y1="508" y2="508" stroke="#dae3e7"/>
<text x="44" y="537" font-size="15" fill="#506974">23 → 6 calls in total. This does not measure model turns, tokens or latency.</text>
</g></svg>`;
await fs.writeFile(new URL('retrieval-calls.svg', folder), svg);
await sharp(Buffer.from(svg)).png().toFile(fileURLToPath(new URL('retrieval-calls.png', folder)));
console.log('Created figures/retrieval-calls.svg and figures/retrieval-calls.png from recorded MCP checks.');

const summary = JSON.parse(await fs.readFile(new URL('./results/live-summary.json', import.meta.url), 'utf8'));
const liveFolder = new URL(`./results/${summary.source}/`, import.meta.url);
const live = await Promise.all((await fs.readdir(liveFolder))
  .filter(name => /^\d+-ORD-\d+-(granular|focused)\.json$/.test(name))
  .map(async name => JSON.parse(await fs.readFile(new URL(name, liveFolder), 'utf8'))));
const variants = ['granular', 'focused'];
const labels = ['Granular', 'Focused'];
const number = value => value.toLocaleString('en-US');

async function saveFigure(name, title, description, body, height = 620) {
  const document = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1100 ${height}" role="img" aria-labelledby="title desc">
<title id="title">${xml(title)}</title><desc id="desc">${xml(description)}</desc>
<rect width="1100" height="${height}" rx="18" fill="#fafbf9"/>
<g font-family="Arial, Helvetica, sans-serif" fill="#18313e">${body}</g></svg>`;
  await fs.writeFile(new URL(`${name}.svg`, folder), document);
  await sharp(Buffer.from(document)).png().toFile(fileURLToPath(new URL(`${name}.png`, folder)));
  console.log(`Created figures/${name}.svg and figures/${name}.png from ${summary.source}.`);
}

let comparison = `<text x="44" y="51" font-size="30" font-weight="700">One fewer model turn, less work per answer</text>
<text x="44" y="83" font-size="17" fill="#506974">${xml(summary.model)} · ${summary.caseCount} synthetic orders × ${summary.repetitions} repetitions per design</text>`;
for (const [panel, metric] of ['totalTokens', 'elapsedMs'].entries()) {
  const left = 90 + panel * 535;
  const isTime = metric === 'elapsedMs';
  const vertical = scaleLinear().domain([0, isTime ? 7 : 4000]).range([435, 170]);
  const reduction = summary.medianReductionPercent[metric].toFixed(1);
  comparison += `<text x="${left}" y="127" font-size="21" font-weight="700">${isTime ? 'Elapsed seconds' : 'Input + output tokens'}</text>`;
  comparison += `<text x="${left}" y="153" font-size="16" fill="#506974">Focused median: ${reduction}% lower</text>`;
  for (const tick of vertical.ticks(isTime ? 7 : 4)) {
    comparison += `<line x1="${left}" x2="${left + 430}" y1="${vertical(tick)}" y2="${vertical(tick)}" stroke="#dae3e7"/>`;
    comparison += `<text x="${left - 14}" y="${vertical(tick) + 5}" text-anchor="end" font-size="14">${number(tick)}</text>`;
  }
  for (const [index, variant] of variants.entries()) {
    const center = left + 105 + index * 210;
    const cases = live.filter(row => row.variant === variant);
    for (const [i, row] of cases.entries()) {
      const value = isTime ? row.elapsedMs / 1000 : row.inputTokens + row.outputTokens;
      const jitter = ((i * 7) % 19 - 9) * 3.2;
      comparison += `<circle cx="${center + jitter}" cy="${vertical(value)}" r="4.5" fill="${colors[index]}" opacity="0.7"/>`;
    }
    const median = summary.variants[variant][metric].median / (isTime ? 1000 : 1);
    comparison += `<line x1="${center - 42}" x2="${center + 42}" y1="${vertical(median)}" y2="${vertical(median)}" stroke="#18313e" stroke-width="3"/>`;
    comparison += `<text x="${center}" y="468" text-anchor="middle" font-size="18" fill="${colors[index]}" font-weight="700">${labels[index]}</text>`;
    comparison += `<text x="${center}" y="495" text-anchor="middle" font-size="20" font-weight="700">${isTime ? median.toFixed(3) + ' s' : number(median)} median</text>`;
  }
}
comparison += `<line x1="44" x2="1056" y1="532" y2="532" stroke="#dae3e7"/>
<text x="44" y="558" font-size="15" fill="#506974">Dots: 18 runs per design. Dark lines: medians. Both designs matched all five scored fields in 18/18 runs.</text>
<text x="44" y="584" font-size="15" fill="#506974">Elapsed time excludes MCP startup/discovery. Local in-memory tools; one small run, not a production forecast.</text>`;
await saveFigure('live-comparison', 'Tokens and elapsed time in the live MCP experiment',
  'Granular median: 3,361 input plus output tokens and 4.044 seconds. Focused median: 1,953 tokens and 2.692 seconds. Each dot represents one of 18 runs per design; dark horizontal lines show medians.', comparison);

const pair = variants.map(variant => live.find(row => row.orderId === 'ORD-1004' && row.repeat === 1 && row.variant === variant));
const time = scaleLinear().domain([0, Math.ceil(Math.max(...pair.map(row => row.elapsedMs)) / 1000) * 1000]).range([90, 1040]);
let timeline = `<text x="44" y="51" font-size="30" font-weight="700">Five tool calls do not mean five model turns</text>
<text x="44" y="83" font-size="17" fill="#506974">ORD-1004 · repetition 1 · laptop delivered, monitor backordered · actual recorded trace</text>`;
for (const tick of time.ticks(6)) {
  timeline += `<line x1="${time(tick)}" x2="${time(tick)}" y1="174" y2="474" stroke="#dae3e7"/>`;
  timeline += `<text x="${time(tick)}" y="501" text-anchor="middle" font-size="15">${tick / 1000}s</text>`;
}
for (const [index, row] of pair.entries()) {
  const top = 180 + index * 170;
  timeline += `<text x="90" y="${top - 22}" font-size="19" font-weight="700" fill="${colors[index]}">${labels[index]} · ${row.toolCalls} tool ${row.toolCalls === 1 ? 'call' : 'calls'} · ${row.modelTurns} model turns · ${(row.elapsedMs / 1000).toFixed(3)} s</text>`;
  const models = row.trace.filter(event => event.kind === 'model');
  for (const [turn, event] of models.entries()) {
    const start = time(event.startMs);
    const width = time(event.startMs + event.durationMs) - start;
    timeline += `<rect x="${start}" y="${top}" width="${width}" height="62" rx="4" fill="${colors[index]}"/>`;
    timeline += `<text x="${start + width / 2}" y="${top + 25}" text-anchor="middle" font-size="17" fill="white">Model ${turn + 1}</text>`;
    timeline += `<text x="${start + width / 2}" y="${top + 48}" text-anchor="middle" font-size="15" fill="white">${(event.durationMs / 1000).toFixed(2)} s</text>`;
    const calls = row.trace.filter(tool => tool.kind === 'tool' && tool.startMs >= event.startMs + event.durationMs && tool.startMs < (models[turn + 1]?.startMs ?? row.elapsedMs));
    if (calls.length) {
      const at = time(calls[0].startMs);
      const span = Math.max(...calls.map(call => call.startMs + call.durationMs)) - calls[0].startMs;
      const label = calls.length === 1 ? calls[0].name : `${calls.length} parallel calls`;
      timeline += `<line x1="${at}" x2="${at}" y1="${top + 60}" y2="${top + 80}" stroke="#18313e"/>`;
      timeline += `<circle cx="${at}" cy="${top + 62}" r="5" fill="#18313e"/>`;
      const labelX = Math.max(220, at);
      timeline += `<text x="${labelX}" y="${top + 101}" text-anchor="middle" font-size="16">${label}</text>`;
      timeline += `<text x="${labelX}" y="${top + 124}" text-anchor="middle" font-size="14" fill="#506974">${span.toFixed(1)} ms ${calls.length === 1 ? 'tool duration' : 'batch span'}</text>`;
    }
  }
}
timeline += `<line x1="44" x2="1056" y1="532" y2="532" stroke="#dae3e7"/>
<text x="44" y="558" font-size="15" fill="#506974">Bars: elapsed model HTTP requests. Dots mark local MCP calls, too brief to show as duration bars at this scale.</text>
<text x="44" y="584" font-size="15" fill="#506974">The four-call batch retrieves payment, two reservations and tracking concurrently. Startup/discovery excluded.</text>`;
await saveFigure('live-order-1004', 'Actual model requests and MCP calls for the partial-delivery case',
  'Granular: three model requests with an order lookup then four parallel lookups, 5.288 seconds total. Focused: two model requests with one context lookup, 2.832 seconds total. Tool execution takes about three milliseconds per batch on in-memory data.', timeline);
