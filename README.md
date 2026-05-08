# ufo_research

An LLM-maintained knowledge base built on top of the U.S. Department of War's
2026 UAP (Unidentified Anomalous Phenomena) declassification release.

The repo pairs **primary sources** (declassified PDFs from FBI, Army, Air Force,
Navy, State Dept, NASA, AARO, etc.) with a **wiki** of summaries, concept pages,
entity pages, and cross-cutting analyses — all written and curated by an LLM
following a fixed schema. The human curates the source set and asks questions;
the LLM does the reading, writing, and linking.

The structure follows the [karpathy/llm-wiki](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f)
pattern.

## Layout

```
raw/                    # source PDFs. Populated by dow_uap_downloader. Gitignored.
dow_uap_downloader/     # .NET 8 CLI that fetches the war.gov UAP manifest
wiki/                   # LLM-maintained knowledge base (Obsidian vault)
  index.md              #   master catalog — start here
  log.md                #   append-only activity log
  summaries/            #   one page per source document
  concepts/             #   phenomena, characteristics, programs
  entities/             #   incidents, units, individuals, agencies, locations
  syntheses/            #   cross-cutting analyses
  journal/              #   session-by-session research entries
SOURCES.md              # provenance: filename → war.gov manifest URL
CLAUDE.md               # instructions for the LLM curator
```

`raw/` is not committed (some PDFs exceed GitHub's 100MB per-file limit and
the corpus is reproducible from the manifest). To populate it from a fresh
clone, run the downloader — see below.

## Sources

All PDFs come from the U.S. Department of War's UAP release manifest:

```
https://www.war.gov/Portals/1/Interactive/2026/UFO/uap-csv.csv
```

See [`SOURCES.md`](SOURCES.md) for the full filename-to-URL list.

## Downloader

`dow_uap_downloader/` is a small .NET 8 console app that fetches the manifest,
extracts unique PDF URLs, and downloads them in parallel with retry/backoff and
resume-by-skip semantics.

```sh
cd dow_uap_downloader
dotnet run -- [concurrency] [outputDir]
#   concurrency: 1..5 parallel downloads (default 4)
#   outputDir:   default ../raw
```

Already-downloaded files are skipped, so re-running is cheap.

## Wiki workflows

Three workflows are defined in [`CLAUDE.md`](CLAUDE.md):

- **Ingest** — read a raw source, create a summary page, update concept/entity
  pages, cross-link, update `index.md` and `log.md`.
- **Query** — start at `wiki/index.md`, navigate the relevant pages, answer
  with wiki-link citations.
- **Lint** — audit the wiki for orphans, contradictions, stale claims, and
  missing cross-links.

Page schema, naming conventions, tagging taxonomy, confidence levels, and the
coverage policy for very large PDFs are all specified in `CLAUDE.md`.

## Status

This is a personal research project. The wiki is partial — see `wiki/index.md`
for what has been ingested so far, and `wiki/log.md` for activity history.

## License

The code and wiki content in this repo are released under the MIT License —
see [`LICENSE`](LICENSE).

The PDFs in `raw/` (when populated by the downloader) are U.S. government
works, not covered by the MIT grant. They are publicly released declassified
documents redistributed via the war.gov manifest referenced in `SOURCES.md`.

