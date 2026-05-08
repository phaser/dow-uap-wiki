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

## [2026-05-08] ingest | 62-HQ-83894 Section 10 — full re-ingest from OCR

First file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 184)`; now `Coverage: full (184 pages)`. The OCR pass fundamentally changed what we know about this section.

### Key correction

- **Date range was wrong**: existing summary stated "predominantly 1966 – October 1967". The file actually spans **August 1966 – June 1977** — eleven years across three FBI directorships (Hoover → L. Patrick Gray III → Clarence Kelley) and four presidents (Johnson → Nixon → Ford → Carter).

### New material added

Eight major new cases / artifacts not previously surfaced:

- **18 Jan 1967, Chesapeake VA — James Collins abduction report** — transparent oblong craft, 4-ft humanoid pilots in trousers and T-shirts, ~8 hours missing time. **Predates the May 1968 *Look Magazine* publication of the Hill case** by 18 months. New entity page: [[entities/james-collins-chesapeake-1967]].
- **26 Apr 1967, Miami FL — Paul L. Peyerl Black-Forest saucer claim** — Luftwaffe test pilot reporting a 1944 photographed 21-ft saucer with "designer KUEHR" and specific bipropellant fuel chemistry (H₂O₂/CH₃OH suggesting Walter HWK 109-509 cross-pollination). New entity page: [[entities/paul-peyerl-1967]].
- **25 Jul 1967, Newark — Ivan T. Sanderson UAO book chapters 13-14** alleging FBI / military impersonation. (Linked from MIB concept page.)
- **Aug 1967 — IGAP / "ufo contact" magazine enclosure** preserves three primary documents otherwise unindexed in this corpus: Lt. Col. George P. Freeman's 17 Mar 1967 letter inviting Steckling to the Pentagon; Paul D. Lowman Jr.'s 20 Feb 1967 NASA Goddard letter to Steckling; and the El Paso Times 5 Mar 1967 story on the **White Sands Missile Range 29-personnel mass sighting (2 Mar 1967)**. New entity page: [[entities/white-sands-march-1967]].
- **19 Mar 1969, Mount Holly NJ — Edward A. Stewart Jr. Pan Am 707 / Elkton MD theory**: alleges UFO "air-mine" downed Pan Am Flight 214 on 8 Dec 1963, escalates to a "Russian missiles camouflaged as flying saucers" theory. Letter cc'd to Nixon, Hoover, two senators, and SecDef Laird.
- **14 May 1969, Del City OK — Larry Stephens MIB letter to Hoover**: earliest documented FBI inquiry on the Men-in-Black folklore in this corpus, with structurally complete folklore detail. New entity page: [[entities/larry-stephens-mib-letter-1969]]; concept page [[concepts/men-in-black]] updated.
- **22 Aug 1974, Milwaukee WI — NMCC inquiry on metal object recovery**: Major Horn at NMCC called FBI within 6 hrs of Milwaukee PD recovering a "13×8×5 inch metallic object with internal heat source"; the Pentagon's command-and-control center inquiring directly is itself notable.
- **14 Jun 1977, Washington DC — Carter White House UFO inquiry**: Stanley Schneider (OSTP) called FBI for Jody Powell asking whether there was any executive-branch coordination on UFO information. Cochran (FBI) replied that there was none.

Plus a copy of the **January 1977 Air Force UFO Fact Sheet** with the full **1947-1969 Project Blue Book sighting tally (12,618 sightings, peak 1952)** — used to seed new entity pages [[entities/project-blue-book]] and [[entities/condon-committee]].

### Cross-page updates

- [[concepts/men-in-black]] — added Stephens 1969 letter and Sanderson 1967 UAO book as documented examples.
- [[entities/socorro-zamora-1964]] — added the December 1966 internal Bureau provenance note acknowledging that the Albuquerque Office produced the original Zamora witness statement.

### Index changes

- Section 10 line in the FBI 62-HQ-83894 catalog updated from "Renaud / Korendor channeling" to a full content gloss.
- 4 new entity pages added under "Specific incidents / events": Collins, Peyerl, White Sands, Stephens.
- 2 new entity pages added under "USAF / USAAF programs": Project Blue Book, Condon Committee.

### Coverage changes

- `62-hq-83894-section-10.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-08] ingest | Army Incident Summaries 173–233 (Box 7) — full re-ingest from OCR

Second file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-18 of 144)` covering Incidents 174-178 only; now `Coverage: full (144 pages)`. The OCR pass surfaced the volume's geographic and analytical character which the small sample missed.

### Key findings

- The volume spans **August–December 1948** (earliest dated event is Incident 187, 19 Aug 1948 Godman AFB; latest is mid-Dec 1948 Perseid-shower attributions in Incident 230). The volume therefore captures the **transitional period** from Project SIGN into Project GRUDGE (formal renaming February 1949).
- **First substantial non-CONUS case load** in the SIGN incident corpus: Hawaii, Japan (Iruka Shima radar+visual; Wakkanai radar), Korea, Philippines (Clark AFB), Germany (Neubiberg), Atlantic Canada (Goose Bay Labrador). Geographic scope expanded materially vs. the CONUS-only earlier volumes.
- **Andrews AFB pursuit case (Incident 207A, 18 Nov 1948)**: 2nd Lt Henry G. Combs USAFR pursued an "oblong ball" with continuous glowing white light at 1700-7500 ft for 10-12 minutes, recorded vertical climbs the object outclimbed, 80–600 mph speed range, dull-gray response when his landing-light was put on it. One of the most operationally substantive single-pilot pursuit cases in the early-program record but absent from the standard canon.
- **Godman AFB Venus case (Incident 187, 19 Aug 1948)**: theodolite-tracked spherical bright silver object at 30,000-40,000 ft over Godman AFB Kentucky — same airfield as the [[entities/mantell-incident|Mantell case]] seven months earlier — definitively identified as the planet **Venus** by Mr. Moore (Head Astrologer, University of Louisville) with MCI verification. Useful additional context for the Mantell-was-Venus debate.
- **Wakkanai Japan (Incident 198, 6 Nov 1948)**: 1 hr 5 min radar circling track over the Wakkanai station (Hokkaido N tip, ~40 km from Soviet-held Sakhalin) evaluated as **Soviet "Ferret" electronic-reconnaissance mission**. One of the earliest unambiguous "UFO → Soviet ELINT" reattributions in the corpus.
- **MIT cosmic-ray balloon misidentification (Incident 194, 3 Nov 1948)**: two 52d Fighter Wing P-51s pursued and Strategic Air Command initially classed as "heavenly body"; subsequently identified as a cluster of eight cosmic-ray research balloons from MIT.
- **First explicit J. Allen Hynek consultancy reference**: the Incident 197/198 cross-reference note re: Comet 1948 V (the "Eclipse Comet" discovered by Dr. Harley Wood, Sydney) directs the case to "Dr. Hynek for his viewpoint" — earliest mention of Hynek by name in this corpus's incident-summary track.
- **Crescent City CA mass sighting (Incidents 200-200c, 17 Oct 1948)**: four independent witnesses (barber, fisherman, dry cleaner, housewife) at separate vantage points, each character-vetted; consistent silver egg/bullet-shape descriptions.

### Cross-page updates

None this round — the major findings touch entities ([[entities/project-sign]], [[entities/project-grudge]], [[entities/mantell-incident]], [[entities/wright-patterson-afb]]) that already exist; new dedicated entity pages for Combs/Andrews-pursuit and Wakkanai-Soviet-Ferret are deferred to keep the per-file ingest tractable.

### Index changes

- Section "Incident Summaries 173–233" line updated from a stub to a full content gloss.

### Coverage changes

- `army-incident-summaries-173-233.md`: `sampled` → `full`; `confidence: medium` → `high`.

## [2026-05-08] ingest | Army Incident Summaries 101–172 (Box 7) — full re-ingest from OCR

Third file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-18 of 178)` covering Incidents 101-102 only; now `Coverage: full (178 pages)`.

### Key findings

- The volume spans **December 1947 – August 1948** with the bulk in **February–July 1948**, bridging the SIGN early-disc-wave catalog (vol. 1-100) and the late-1948 transitional phase (vol. 173-233).
- **Bakersfield CA March 1948 burning-aircraft wave (Incidents 106-109)**: multi-source "falling aircraft on fire with red-and-black smoke" descriptions sometimes including a parachute drifting eastward; sheriff's office, USAF, and rescue searches found no debris. Investigated by Lt Col Donald L. Springer (4th Air Force, Hamilton Field).
- **Ashley/Delaware OH 8 April 1948 cylinder cluster (Incidents 112-112c)**: six independent witnesses across two towns ~10 mi apart described a slow-moving daylight cylindrical object with vapor streamers; one witness (Mrs. Selah Stephens) used the phrase "opalescent like mother of pearl"; pastor and newspaper-reporter among the witnesses; Delaware airport alerted but never detected arrival.
- **Selfridge Field MI 25 + 28 May 1948 paired sightings (Incidents 134, 135)**: Lt Kokolonis (Corps of Engineers) reported 5 silvery-gold disc-shaped objects in stepped-up line-astern formation, 300-400 ft size at 8,000 ft, 500+ mph; M/Sgt Ernest Davis Jr. reported a single 4-ft brass-colored sphere from the same area three days later.
- **Rapid City AFB / 28th Bomb Group August 1948**: Maj Elmer H. Hamer (Intelligence Officer) reported approximately 12 yellowish-white elliptical objects in tight diamond formation, 100+ ft length, 500+ mph — one of the largest formation reports in the SIGN corpus.
- **Scandinavian "ghost rocket" continuity (Incidents 132-133)**: December 1947 Oslo + February 1948 Norway/Denmark/Sweden objects "with green tails... from the direction of Peenemünde" — over a year and a half after the famous 1946 Swedish wave, suggesting the [[concepts/ghost-rockets|ghost-rocket]] phenomenon persisted. Investigator note: "trend ... to appear at 2130 hours might be significant."
- **Pacific theater first pilot pursuit (Incident 111)**: 1st Lt Robert L. Meyers, 67 FS / 18 FG, observed two silver "flying-wing-type" objects ~30 ft × 20 ft over the Philippines on 1 April 1948.
- **First citizen "foreign-aircraft" sound report (Incident 110)**: anonymous Hamilton MD housewife reports unusual nighttime aircraft-sounds via FBI Baltimore → Second Army → AMC. The cold-war framing distinguishes this from the "flying disc" framing dominant elsewhere in the volume.
- **Oak Ridge TN adjacency (Incident 136a)**: Tryus W. Setliff (Oakridge TN) co-witnessed the 30 June 1948 ball-of-fire over Knoxville — early documentation of an [[concepts/atomic-energy-site-overflight|atomic-energy site]] adjacency case.

### Cross-page updates

None this round (the major findings touch existing entities; new dedicated entity pages for the Selfridge formation case, the Rapid City diamond formation, and the Bakersfield wave are deferred).

### Index changes

- "Incident Summaries 101–172" line updated from "Norcatur fireball, La Paz vs. Markham" stub to a full content gloss.

### Coverage changes

- `army-incident-summaries-101-172.md`: `sampled` → `full`; `confidence: medium` → `high`.


