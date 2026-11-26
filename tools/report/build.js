// report.md -> IEEE conference style A4 .docx (title block in one column, body in two columns).
// Usage: node build.js <report.md> <fig1.png> <out.docx>
const fs = require("fs");
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell, ImageRun, AlignmentType, WidthType,
  BorderStyle, LevelFormat, SectionType, VerticalAlign,
} = require("docx");

const [mdPath, figPath, outPath] = process.argv.slice(2);
const lines = fs.readFileSync(mdPath, "utf8").replace(/\r/g, "").split("\n");

// A4, IEEE margins: top 19 mm, left/right 14.32 mm; bottom 25.4 mm; column gap 4.22 mm
const PAGE = { width: 11906, height: 16838 };
const MARGIN = { top: 1077, bottom: 1440, left: 812, right: 812 };
const GAP = 239;
const COL = Math.floor((PAGE.width - MARGIN.left - MARGIN.right - GAP) / 2);  // twips
const FONT = "Times New Roman", MONO = "Courier New";

// ---------- inline text ----------
function typo(t) {
  return t.replace(/ -> /g, " → ").replace(/->/g, "→").replace(/<=/g, "≤").replace(/>=/g, "≥")
    .replace(/(^|[\s(\[—-])"/g, "$1“").replace(/"/g, "”")
    .replace(/(\w)'(\w)/g, "$1’$2").replace(/(^|\s)'/g, "$1‘").replace(/'/g, "’");
}

function runs(text, base = {}) {
  const out = [];
  const parts = text.split(/(`[^`]*`)/);
  let bold = false, italic = false;
  for (const part of parts) {
    if (!part) continue;
    if (part.startsWith("`") && part.endsWith("`") && part.length >= 2) {
      out.push(new TextRun({ text: part.slice(1, -1).replace(/([\/_.])/g, "$1​"), font: MONO, size: Math.round((base.size || 20) * 0.85), bold, italics: italic }));
      continue;
    }
    // ** toggles bold, * toggles italic
    let buf = "";
    const flush = () => { if (buf) { out.push(new TextRun({ text: typo(buf), font: FONT, bold: bold || base.bold, italics: italic || base.italics, size: base.size, smallCaps: base.smallCaps })); buf = ""; } };
    for (let i = 0; i < part.length; i++) {
      // CommonMark-like flanking: "A* on a grid" is a literal star (a space after it cannot open, a space before it cannot close)
      const dbl = part[i] === "*" && part[i + 1] === "*";
      const before = part[i - 1] || " ", after = part[dbl ? i + 2 : i + 1] || " ";
      const canOpen = part[i] === "*" && !/\s/.test(after) && !/[A-Za-z0-9]/.test(before);
      const canClose = part[i] === "*" && !/\s/.test(before) && (dbl || !/[A-Za-z0-9]/.test(after));
      if (dbl && ((bold && canClose) || (!bold && canOpen))) { flush(); bold = !bold; i++; }
      else if (!dbl && part[i] === "*" && ((italic && canClose) || (!italic && canOpen))) { flush(); italic = !italic; }
      else buf += part[i];
    }
    flush();
  }
  return out;
}

// ---------- block parsing ----------
const blocks = [];
for (let i = 0; i < lines.length;) {
  const l = lines[i];
  if (!l.trim()) { i++; continue; }
  if (l.startsWith("```")) {
    const code = []; i++;
    while (i < lines.length && !lines[i].startsWith("```")) code.push(lines[i++]);
    i++; blocks.push({ type: "code", lines: code }); continue;
  }
  if (l.startsWith("# ")) { blocks.push({ type: "title", text: l.slice(2) }); i++; continue; }
  if (l.startsWith("## ")) { blocks.push({ type: "h1", text: l.slice(3) }); i++; continue; }
  if (l.startsWith("|")) {
    const rows = [];
    while (i < lines.length && lines[i].startsWith("|")) {
      const cells = lines[i].trim().replace(/^\||\|$/g, "").split("|").map(c => c.trim());
      if (!cells.every(c => /^:?-+:?$/.test(c))) rows.push(cells);
      i++;
    }
    blocks.push({ type: "table", rows }); continue;
  }
  if (/^- /.test(l)) {
    const items = [];
    while (i < lines.length && (/^- /.test(lines[i]) || (/^  \S/.test(lines[i]) && items.length))) {
      if (/^- /.test(lines[i])) items.push(lines[i].slice(2)); else items[items.length - 1] += " " + lines[i].trim();
      i++;
    }
    blocks.push({ type: "bullets", items }); continue;
  }
  if (/^\d+\. /.test(l)) {
    const items = [];
    while (i < lines.length && (/^\d+\. /.test(lines[i]) || (/^   \S/.test(lines[i]) && items.length))) {
      if (/^\d+\. /.test(lines[i])) items.push(lines[i].replace(/^\d+\. /, "")); else items[items.length - 1] += " " + lines[i].trim();
      i++;
    }
    blocks.push({ type: "numbers", items }); continue;
  }
  const para = [];
  while (i < lines.length && lines[i].trim() && !/^(```|#|\||- |\d+\. )/.test(lines[i])) para.push(lines[i++].trim());
  blocks.push({ type: "p", text: para.join(" ") });
}

// ---------- rendering ----------
const head = [], body = [];
let numberInstance = 0, inRefs = false, figDone = false;

const P = (children, opts = {}) => new Paragraph({ children, alignment: AlignmentType.JUSTIFIED, spacing: { after: 0, line: 240 }, indent: { firstLine: 202 }, ...opts });

function caption(text) {   // "*Table II. Final ...*" -> TABLE II. Final ...
  const m = text.match(/^\*(Table [IVX]+)\.\s*(.*?)\*?\s*$/);
  if (!m) return null;
  return new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 120, after: 60 }, keepNext: true,
    children: [new TextRun({ text: m[1].toUpperCase() + ". ", font: FONT, size: 16, smallCaps: true }), ...runs(m[2].replace(/\*$/, ""), { size: 16 })] });
}

function table(rows) {
  const n = rows[0].length;
  const len = c => Math.min(40, Math.max(4, c.replace(/[`*]/g, "").length));
  const weight = Array.from({ length: n }, (_, j) => Math.max(...rows.map(r => len(r[j] || ""))));
  const sum = weight.reduce((a, b) => a + b, 0);
  const widths = weight.map(w => Math.floor(COL * w / sum));
  widths[n - 1] += COL - widths.reduce((a, b) => a + b, 0);
  const size = n >= 6 ? 14 : 16;
  const border = { style: BorderStyle.SINGLE, size: 4, color: "444444" };
  const borders = { top: border, bottom: border, left: border, right: border };
  return new Table({
    width: { size: COL, type: WidthType.DXA }, columnWidths: widths,
    rows: rows.map((r, ri) => new TableRow({
      tableHeader: ri === 0, cantSplit: true,
      children: r.map((c, j) => new TableCell({
        width: { size: widths[j], type: WidthType.DXA }, borders, verticalAlign: VerticalAlign.CENTER,
        margins: { top: 20, bottom: 20, left: 50, right: 50 },
        children: [new Paragraph({ alignment: j === 0 || (ri > 0 && c.length > 30) ? AlignmentType.LEFT : AlignmentType.CENTER, spacing: { after: 0 },
          children: runs(c, { size, bold: ri === 0 }) })],
      })),
    })),
  });
}

for (let b = 0; b < blocks.length; b++) {
  const k = blocks[b], next = blocks[b + 1];
  if (k.type === "title") {
    head.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [new TextRun({ text: typo(k.text), font: FONT, size: 48 })] }));
    continue;
  }
  if (head.length && head.length < 4 && k.type === "p" && !body.length) {   // subtitle and author lines
    const a = k.text.match(/Author: ([^,]+), (\d+)\. Team: (.*)\./);
    if (a) {
      head.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 20 }, children: [new TextRun({ text: a[1], font: FONT, size: 22 })] }));
      head.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 20 }, children: [new TextRun({ text: "Student no. " + a[2] + ", " + a[3], font: FONT, size: 20, italics: true })] }));
      head.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 360 }, children: [new TextRun({ text: "CSE-377 Game Programming, final project report", font: FONT, size: 20, italics: true })] }));
    }
    continue;
  }
  if (k.type === "h1") {
    if (k.text === "Abstract") {
      const abs = next; b++;
      body.push(new Paragraph({ alignment: AlignmentType.JUSTIFIED, spacing: { after: 120 }, children: [new TextRun({ text: "Abstract—", font: FONT, size: 18, bold: true, italics: true }), ...runs(abs.text, { size: 18, bold: true })] }));
      body.push(new Paragraph({ alignment: AlignmentType.JUSTIFIED, spacing: { after: 120 }, children: [new TextRun({ text: "Index Terms—", font: FONT, size: 18, bold: true, italics: true }),
        new TextRun({ text: "real-time strategy, multiplayer games, Photon Fusion, Model Context Protocol, large language models, retrieval-augmented generation, human-in-the-loop, game security.", font: FONT, size: 18, bold: true })] }));
      continue;
    }
    inRefs = k.text === "References";
    body.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 200, after: 100 }, keepNext: true,
      children: [new TextRun({ text: typo(k.text), font: FONT, size: 20, smallCaps: true })] }));
    continue;
  }
  if (k.type === "p" && /^\*Fig\. 1\./.test(k.text) && next && next.type === "code") {   // the ASCII diagram becomes the figure
    body.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 120, after: 60 }, keepNext: true,
      children: [new ImageRun({ type: "png", data: fs.readFileSync(figPath), transformation: { width: 331, height: 398 },
        altText: { title: "Fig. 1", description: "Architecture: Unity, AI gateway, Ollama and the MCP server", name: "fig1" } })] }));
    body.push(new Paragraph({ alignment: AlignmentType.JUSTIFIED, spacing: { after: 160 },
      children: [new TextRun({ text: "Fig. 1. ", font: FONT, size: 16 }), ...runs("Architecture and the path of one question. Unity sends a visibility-filtered snapshot; the gateway's agent loop calls read-only MCP tools, checks the plan and returns it; the player approves and the host validates again.", { size: 16 })] }));
    b++; figDone = true; continue;
  }
  if (k.type === "p" && next && next.type === "table" && caption(k.text)) { body.push(caption(k.text)); continue; }
  if (k.type === "p" && inRefs) {
    body.push(new Paragraph({ alignment: AlignmentType.JUSTIFIED, spacing: { after: 40 }, indent: { left: 360, hanging: 360 }, children: runs(k.text, { size: 16 }) }));
    continue;
  }
  if (k.type === "p") { body.push(P(runs(k.text), next && (next.type === "table" || next.type === "code") ? { keepNext: true } : {})); continue; }
  if (k.type === "table") { body.push(table(k.rows)); body.push(new Paragraph({ spacing: { before: 0, after: 60, line: 120 }, children: [] })); continue; }
  if (k.type === "bullets" || k.type === "numbers") {
    const inst = ++numberInstance;
    for (const it of k.items)
      body.push(new Paragraph({ alignment: AlignmentType.JUSTIFIED, spacing: { after: 20, line: 240 },
        numbering: { reference: k.type, level: 0, instance: inst }, children: runs(it) }));
    continue;
  }
  if (k.type === "code") {
    for (const cl of k.lines)
      body.push(new Paragraph({ spacing: { after: 0, line: 220 }, indent: { left: 0 }, children: [new TextRun({ text: cl || " ", font: MONO, size: 13 })] }));
    body.push(new Paragraph({ spacing: { after: 80 }, children: [] }));
  }
}
if (!figDone) throw new Error("Fig. 1 not found");

const listLevel = (format, text) => ({ reference: format, levels: [{ level: 0, format: format === "bullets" ? LevelFormat.BULLET : LevelFormat.DECIMAL,
  text, alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 288, hanging: 200 } } } }] });

const doc = new Document({
  creator: "Erkan Ceylan", title: "An Agentic Multiplayer RTS", description: "CSE-377 final project report",
  styles: { default: { document: { run: { font: FONT, size: 20 } } } },
  numbering: { config: [listLevel("bullets", "•"), listLevel("numbers", "%1)")] },
  sections: [
    { properties: { page: { size: PAGE, margin: MARGIN } }, children: head },
    { properties: { type: SectionType.CONTINUOUS, page: { size: PAGE, margin: MARGIN }, column: { count: 2, space: GAP, equalWidth: true } }, children: body },
  ],
});
Packer.toBuffer(doc).then(buf => { fs.writeFileSync(outPath, buf); console.log("wrote", outPath, buf.length, "bytes"); });
