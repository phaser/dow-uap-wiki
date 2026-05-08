# Activity Log

Append-only chronological record of ingests, queries, syntheses, and lint passes.

---

## [2026-05-08] bootstrap | Wiki initialized

- Created directory layout: `raw/`, `wiki/{summaries,concepts,entities,syntheses,journal}/`
- Authored `CLAUDE.md` schema (page types, frontmatter, naming, tagging, coverage policy)
- Hard-linked 112 PDFs from `downloads/` into `raw/`
- Initialized `index.md` and `log.md` skeletons
- Seeded by user request: "create a llmwiki at ../ according to karpathy gist + ~/projects/llm-wiki"

## [2026-05-08] ingest | Bulk first-pass ingest of all 112 raw PDFs

- Strategy: parallel agent batches grouped by provenance (FBI photos, FBI Hottel-era files, Army incident summaries, State Dept cables, DoW/AARO mission reports, NASA transcripts, misc)
- Coverage: per-document summaries follow the **Coverage Policy** in `CLAUDE.md` — small docs read in full, large compilations (incident summaries vol. 1-100 / Project Blue Book volumes / FBI 62-HQ-83894 sections) sampled and indexed at the corpus level
- See individual `summaries/` pages for per-source coverage marks

### Result

- **105 summaries** written (one per raw source; the duplicate `59_214434_sp_16_*` was folded into a single summary, MD5-confirmed identical)
- **39 concept pages** seeded covering foundational terminology (uap, flying-disc, foo-fighter, range-fouler, transmedium, orb, ghost-rockets, unconventional-aircraft), morphology / behavior, sensor / signature classes, methodology / disposition, physical-trace evidence, site clustering, orbital / lunar phenomena, cultural / contactee, policy / diplomatic
- **41 entity pages** seeded covering agencies (FBI, NASA, State, AARO, AMC), USAF programs (SIGN, GRUDGE, TWINKLE), units (415 NFS, 482 ATKS), missions (Apollo 11/12/17, Skylab), bases (Muroc, Hamilton, Clinton County, Wright-Patterson), specific events (Maury Island, Mantell, Socorro, Borman GT-7, FBI 1999-12-31 sequence, 2025 Western US helicopter orb), reports / policy docs (COMETA 1999, Hunter NASC 1963), and key individuals (Arnold, Strapp/Stapp, Cabell, La Paz, Markham)
- **4 syntheses** written: 1947-flying-disc-wave, uap-terminology-evolution, 2020-arabian-gulf-cluster, fbi-institutional-posture

### Toolchain notes for future runs

- The Read tool's PDF support has a **32MB request size limit**: for any PDF >8MB on disk, agents must always pass an explicit `pages` parameter. Many encrypted-and-image-only declassified PDFs (FBI 62-HQ-83894, Army Box 7 incident summaries, etc.) require an external OCR pipeline. Working pipelines verified by ingest agents:
  - `qpdf --decrypt → pdftoppm @ 200 dpi → tesseract` (handles encrypted scanned PDFs)
  - Direct `pdftotext` for files with intact text layers
  - Ghostscript + Tesseract for non-encrypted scanned PDFs
- A reusable script lives at `/tmp/ocr_pdf.sh` (built by an ingest agent during the run; survives the session). Useful tools installed during this run: `qpdf`, `tesseract`, `poppler` (`/opt/homebrew/bin/pdftoppm`), `pypdf`, `cryptography`, `pdf2image`, `pytesseract`.

### Known gaps flagged in summaries

- Guy Hottel memo (22 March 1950) not surfaced in the sampled spans of FBI 62-HQ-83894 sections 5–6 — needs a targeted full-section OCR pass.
- Mantell core narrative (Box 7 incident-summary "33-series") not in sampled pages of `army-incident-summaries-1-100`.
- Roswell July 1947 records not located in the corpus (referenced in COMETA Report Appendix 5 only).
- Several DoW UAP filenames misalign with their content (D14, D20, D27, D28, D42, D60); flagged per-summary.

## [2026-05-08] ocr | Persistent OCR pipeline + raw/ocr/ committed

User requested a sustainable way to "transform large PDFs into manageable ones". The bulk-ingest run had been using ad-hoc OCR helpers (the `/tmp/ocr_pdf.sh` mentioned above) which didn't survive across sessions; large scans were sampled rather than read in full per the Coverage Policy.

### Pipeline

- Tooling: `ocrmypdf` (Tesseract under the hood), installed via Homebrew. Single command per file:
  ```sh
  ocrmypdf --skip-text --rotate-pages --deskew \
           --jobs 4 --output-type pdf --optimize 1 --quiet \
           raw/<name>.pdf raw/ocr/<name>.pdf
  ```
  - `--skip-text` passes through pages that already have a text layer (cheap on the rare doc that wasn't a pure scan).
  - `--rotate-pages` + `--deskew` correct flipped / skewed scan pages.
  - `--optimize 1` produces moderately compressed output (final corpus 1.9 GB OCR'd vs 2.3 GB raw).
- Wrapper: `scripts/ocr.sh` — iterates `raw/*.pdf`, idempotent (skips files already in `raw/ocr/`), per-file logs at `raw/ocr/.logs/<name>.log`.
- Survey of OCR quality across the previously-`sampled` files: clean-word ratio 89–95 %; word density per page varies an order of magnitude (FBI HQ section 10 ≈ 370 words/page vs Army incident-summary forms ≈ 130 words/page).

### Outputs

- 112 OCR'd PDFs in `raw/ocr/<name>.pdf` (preserves visual layout + adds searchable text layer).
- Plain-text sidecars at `raw/ocr/.txt/<name>.txt` (used by `scripts/triage.sh` and direct inspection — much faster than re-OCR'ing).
- 4 PDFs still exceed GitHub's 100 MB per-file limit after OCR (`section_6`, `incident_summaries_1-100`, `incident_summaries_101-172`, `box186 flying-discs-1949`); excluded from git, regenerable locally via `scripts/ocr.sh`. Their `.txt` extracts in `raw/ocr/.txt/` are committed, so the searchable content is in the repo even when the PDF isn't.
- `.gitignore` keeps the original `raw/` scans excluded but un-ignores the OCR'd subtree (minus the four oversize files).

### Triage

`scripts/triage.sh` produces a per-file table of pages, OCR'd char count, line- and chunk-level uniqueness, date-pattern density, and incident-marker counts. Used to rank the 17 `Coverage: sampled` summaries for re-ingest priority. First-pass redundancy detection (line + 200-char chunk hashing) was defeated by OCR-noise variance — semantic dedup deferred. Top-priority files identified by structural-marker density:

1. `62-hq-83894-section-10` — densest correspondence (120 "flying saucer" mentions, 184 pages)
2. `army-incident-summaries-173-233` — densest case-by-case (109 INCIDENT markers)
3. `army-incident-summaries-101-172` — full CHECK-LIST forms throughout
4. `62-hq-83894-section-1` — sets the FBI-section template; insights generalize
5. `army-incident-summaries-1-100` — completes the Army incident trio

Re-ingestion using these OCR'd texts begins after this log entry.


