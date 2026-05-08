## Purpose

This is an LLM-maintained knowledge base on **UFO / UAP (Unidentified Aerial / Anomalous Phenomena)** primary-source documents — declassified U.S. government records (FBI, Army, Air Force, State Dept, NASA, DoW/AARO, etc.) sitting in `raw/`. The LLM writes and maintains all files under `wiki/`. The human curates raw sources in `raw/` and directs queries. The human never edits wiki files directly.

The pattern follows the [karpathy/llm-wiki](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) gist and mirrors the schema used in `~/projects/claude_wiki`.

## Directory Layout

- `raw/` — Immutable source documents (declassified PDFs, transcripts, photos). **Never modify.**
- `wiki/index.md` — Master catalog. Every wiki page must appear here.
- `wiki/log.md` — Append-only activity log.
- `wiki/summaries/` — One summary page per raw source document.
- `wiki/concepts/` — Phenomena, characteristics, programs, methodologies (e.g., `flying-disc`, `range-fouler`, `project-blue-book`, `foo-fighter`).
- `wiki/entities/` — Specific incidents, units, individuals, agencies, locations, craft types (e.g., `fbi`, `aaro`, `apollo-11`, `roswell-incident`, `nimitz-encounter`).
- `wiki/syntheses/` — Cross-cutting analyses, era comparisons, pattern studies, geographic/temporal clusters.
- `wiki/journal/` — Session-by-session research entries (not used for sources).

## File Naming

- All lowercase, hyphens for word separation: `concept-name.md`
- No spaces, no special characters, no uppercase
- Summary slugs derive from the raw filename, but normalized: `255_413270_ufos_and_defense.md` (drop punctuation, keep agency/series prefix when present)
- Name should match the page title slug

## Page Format

Every wiki page uses this frontmatter and structure:

```yaml
---
title: "Page Title"
type: summary | concept | entity | synthesis | journal
tags: [tag1, tag2, tag3]
created: YYYY-MM-DD
updated: YYYY-MM-DD
sources: ["raw/filename.pdf"]
confidence: high | medium | low
---
```

### Required Sections by Page Type

**Summary pages** (`wiki/summaries/`):
- `## Source Metadata` — Originating agency, document series/identifier, date(s) of records, page count, classification markings, file size, raw path
- `## Coverage` — `full` | `partial (pages X-Y of N)` | `sampled` — declare honestly when a doc was too long to read end-to-end
- `## Key Points` — Bulleted list of concrete claims, incidents, names, dates, locations, technical details
- `## Notable Incidents / Cases` — When the doc contains discrete sighting reports, list them with date / location / witness / object description
- `## Relevant Concepts` — Wiki links to concept pages this source touches
- `## Relevant Entities` — Wiki links to entity pages (agencies, units, named witnesses, named incidents)
- `## Open Questions` — Things the source raises but doesn't resolve

**Concept pages** (`wiki/concepts/`):
- `## Definition` — One-paragraph plain-English definition
- `## Characteristics` — Observed properties, parameters, signatures
- `## Documented Examples` — Bulleted incidents with links to summary or entity pages
- `## Related Concepts` — Wiki links
- `## Sources` — Raw sources informing this page (markdown code spans, not wiki-links)

**Entity pages** (`wiki/entities/`):
- `## Overview` — What/who/where this is
- `## Role / Activity` — How this entity figures into the UAP record
- `## Documented in Sources` — Bullet list of `[[summaries/...]]` where the entity appears
- `## Related Entities` — Wiki links
- `## Related Concepts` — Wiki links

**Synthesis pages** (`wiki/syntheses/`):
- `## Scope` — What sources/topics this synthesis spans
- `## Findings` — Cross-cutting observations, patterns, contradictions
- `## Comparison Table` — When applicable
- `## Pages Compared` — Links to all summaries/entities/concepts involved
- `## Open Threads` — Where the record is thin or contradictory

## Linking Conventions

- Use Obsidian-style wiki links with relative paths from wiki root: `[[concepts/flying-disc]]`, `[[entities/fbi]]`, `[[summaries/fbi-photo-b1]]`, `[[syntheses/1947-flying-disc-wave]]`
- Every page must link to at least one other page (no orphans)
- When mentioning a concept/entity that has a page, always link it
- Raw-source references use plain markdown code spans or the `sources:` frontmatter — not `[[]]` (raw files are not wiki pages)

## Tagging Taxonomy

- **Era**: `pre-1947`, `1947-wave`, `1948-1955`, `1960s`, `1970s`, `1980s`, `1990s`, `2000s`, `2010s`, `2020s`
- **Agency**: `fbi`, `army`, `air-force`, `navy`, `state-dept`, `nasa`, `dow`, `aaro`, `cia`
- **Phenomenon**: `flying-disc`, `foo-fighter`, `range-fouler`, `transmedium`, `orb`, `cigar`, `triangle`
- **Source Type**: `cable`, `mission-report`, `incident-summary`, `transcript`, `photograph`, `correspondence`, `slide-deck`, `debriefing`
- **Region**: `conus`, `middle-east`, `arabian-gulf`, `mediterranean`, `western-pacific`, `europe`, `south-pacific`, `lunar`, `orbital`
- **Status**: `declassified`, `redacted`, `partial-coverage`

## Confidence Levels

- **high** — Source is fully read, claims are direct quotes or close paraphrases of unambiguous text
- **medium** — Source partially read or contains heavy redactions; claims inferred from context
- **low** — Doc was sampled only / OCR-poor / heavily redacted; claims are speculative

## Coverage Policy (large PDFs)

Many raw PDFs are hundreds of MB and span thousands of pages. The Read tool reads at most 20 pages per request.

- For documents ≤ 50 pages: read in full, mark `Coverage: full`.
- For documents > 50 pages: read at minimum the first 30 pages + a sample of later pages (cover letters, summaries, indexes if present). Mark `Coverage: partial (pages 1-30, 100-120 of N)`.
- For very large compilations (incident summaries vol. 1-100 etc.): treat as a corpus — sample the first 40 pages to characterize structure and produce a summary that describes contents at the *index* level, naming representative incidents. Mark `Coverage: sampled`.
- Honesty over completeness — never fabricate content past what was actually read.

## Workflows

### Ingest

When a file lands in `raw/` or the user says "ingest [source]":

1. Read the raw source per the **Coverage Policy**.
2. Create `wiki/summaries/<source-slug>.md` with full schema-compliant summary.
3. Identify concepts, entities, incidents mentioned.
4. For each concept/entity: create the page if absent, or update with new info if it exists. Add the new summary to `## Documented in Sources` / `## Documented Examples`.
5. Add cross-links in both directions between all touched pages.
6. Update `wiki/index.md` — add new entries.
7. Append to `wiki/log.md` with date, source, pages created/updated.
8. Flag any contradictions with existing wiki content.

### Query

When the user asks a question:

1. Read `wiki/index.md` to find relevant pages.
2. Read those pages.
3. Synthesize an answer citing specific pages with wiki links.
4. If the answer reveals new insight worth preserving, create a synthesis page in `wiki/syntheses/` and update index + log.

### Lint

When the user says "lint" or "health check":

1. Walk all wiki pages.
2. Check for: orphan pages, stale claims, contradictions, missing cross-links, incomplete sections, low-confidence pages that could be strengthened by re-reading the source.
3. Fix what can be fixed automatically.
4. Report issues that need human judgment.
5. Update log.

## Rules

- Never modify files in `raw/`.
- Always update `index.md` and `log.md` after any wiki change.
- Prefer updating existing pages over creating duplicates.
- When in doubt about a claim, set `confidence: low` and note the uncertainty.
- Keep pages focused — one concept per page; split long pages.
- Plain English — define jargon (e.g., AARO, UAP, range-fouler) on first use in each page.
- All dates ISO 8601: `YYYY-MM-DD`.
- Distinguish **what the document says** from **what is true** — primary sources record reports and beliefs; the wiki preserves that distinction (e.g., "the FBI memo states X" vs. "X happened").
- Note redactions explicitly: `[REDACTED]` markers in the source should be preserved as such in summaries.
