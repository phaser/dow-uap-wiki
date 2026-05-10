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

### Outputs (after the layout consolidation — see below)

- 112 OCR'd PDFs in `raw/<name>.pdf` (replaced the original scans in place; visual layout preserved, searchable text layer added).
- 112 plain-text extracts in `raw/ocr/<name>.txt` (~10 MB total, the canonical grep-able form of the corpus).
- `raw/*.pdf` is gitignored (~1.9 GB local-only, regenerable from the war.gov manifest via `dow_uap_downloader` + `scripts/ocr.sh`). `raw/ocr/*.txt` is committed.

### Layout consolidation

Initial structure was `raw/<name>.pdf` (originals) + `raw/ocr/<name>.pdf` (OCR'd) + `raw/ocr/.txt/<name>.txt` (per-file plaintext, only generated for the 6 triaged files). User pointed out the OCR'd PDFs were still large (OCR adds a text layer; it doesn't shrink the page rasters) and asked for the cleaner layout described above: replace originals with OCR'd in place, drop the duplicate PDF tree, keep only the per-file text extract flat under `raw/ocr/`. Repo dropped from ~1.7 GB of tracked OCR'd PDFs to ~10 MB of tracked text extracts.

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

## [2026-05-09] ingest | 62-HQ-83894 Section 1 — full re-ingest from OCR

Fourth file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 185)`; now `Coverage: full (185 pages)`.

### Key findings

- **Schulgen / Reynolds 9 July 1947 cooperation negotiation** preserved verbatim. Brig. Gen. George F. Schulgen (Chief, Requirements Intelligence Branch, AAC Intelligence) "assured Mr. Reynolds that there are no War Department or Navy classified research projects presently being conducted which could in any way be tied up with the flying disks" — at the exact moment the Roswell-area events were unfolding.
- **Hoover's marginal notation** on the cooperation memo: "I would do it but before agreeing to it we must insist upon full access to discs recovered. For instance in the **Ia. case the Army grabbed it and would not let us have it for cursory examination**." The "Iowa case" reference is unidentified within this section — material lead worth tracing.
- **Bureau Bulletin No. 42, 30 July 1947** — formal directive to all field offices to investigate flying-disc reports under AAF cooperation; followed by an internal early-August ADDENDUM where the Bureau began stepping back ("a great bulk of those alleged discs reported found have been pranks").
- **Davidson + Brown Kelso B-25 crash, 1 August 1947** — Capt. William L. Davidson and Lt Frank M. Brown, 4th AAF HQ San Francisco, killed when their plane crashed at Kelso WA returning from interviews (27 July 1947) with Kenneth Arnold, Capt. E. J. Smith / Ralph Stevens (United Airlines), Dave Johnson (Idaho Statesman), and Dick Rankin. AAF Intelligence McChord "screened" the wreckage; Tacoma News Tribune reported the plane was carrying parts of a Maury Island disc; AAF analysis later identified fragments as Tacoma-slag-mill material.
- **Portland Police 4 July 1947 multi-officer event** — formal CIC 6th Army memorandum by SA Keith A. Sorensen; multiple officer witnesses (Lissy + Ellis Car #82, Patterson Car #15, McDowell Precinct #1's pigeons, Harbor Patrol Capt. Prehn, Sergeant Cross Oregon State Police).
- **William Rhoads Phoenix photographs (7 July 1947)** preserved as Exhibits 1+2 of CIC-AAF SA Lynn C. Aldrich's memorandum; canonical photographic primary record.
- **Bill Turrentine Norfolk VA photograph (8 July 1947)** — 15-year-old's third-floor-porch shot of an oval/football object at 1/100 sec; published in Norfolk Ledger-Dispatch; SA Thomas J. Connor interview 8 August 1947.
- **Mrs. Gwynne M. Merchant / Santa Fe NM thread** — repeat-visitor to the Santa Fe Resident Agency in July 1947 reporting flying-object phenomena around Park View, Tierra Amarilla, and Canjilon NM; discussed with **Sidney Newburger, Chief of Security and Intelligence for the Atomic Energy Commission**. Earliest AEC / Los Alamos-security entanglement with the UFO topic in this corpus.
- **The "Memorandum of Importance" from 3615 Pine Place, San Diego CA (8 July 1947)** — anonymous one-page treatise on the **etheric / interpenetrating-plane theory of UFO origin** ("etheric planet which interpenetrates with our own", "Lokas or Talas", "apports", "vibratory rate of our dense matter", visitors as "human-like but much larger in size"). Address 3615 Pine Place matches Meade Layne / Borderland Sciences Research Foundation. Earliest preserved primary-source statement of the etheric/interdimensional UFO hypothesis in the FBI's primary file — arriving the same week as the Schulgen-Reynolds negotiation.
- **Hoax inundation series** preserved in detail: Brasky Grafton WI saw blade ("priest had been drinking quite heavily"), Parker Laurel MD Gulf-Oil-sign + garbage-can, Hanson Black River Falls WI cardboard, East St. Louis IL pressed-paper locomotive packing washers, Birmingham AL carnival searchlights, Lewiston ID weed seeds, Lodi CA crop duster.

### Cross-page updates

None this round; major findings touch existing entities ([[entities/fbi]], [[entities/j-edgar-hoover]], [[entities/kenneth-arnold]], [[entities/rhodes-phoenix-photographs]], [[entities/maury-island-affair]], [[entities/harold-dahl]]). Dedicated entity pages for the Davidson-Brown Kelso crash, the Mrs. Merchant / Sidney Newburger AEC liaison, and the "Memorandum of Importance" / Meade Layne BSRF attribution are deferred but flagged as natural follow-ups.

### Index changes

- "Section 1" line updated from a stub to a full content gloss.

### Coverage changes

- `62-hq-83894-section-1.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-09] ingest | 62-HQ-83894 Section 7 (205 pages, full read)

Ninth file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 205)`; now `Coverage: full (205 pages OCR'd)`.

### Key findings

- **Summer 1952 wave** — year of the Washington National flap; greatly intensified saucer reporting nationwide; FBI mostly doing acknowledgement letters routed to OSI.
- **Savannah River Plant AEC sighting (Aug 8, 1952, ~9:30 pm)** — two E. I. du Pont employees observe blue light with orange fringe, "shaped like a saucer," over 400 area at high speed NE.
- **German V-weapon saucer letter (Aug 1952, Cincinnati Enquirer)** — anonymous letter in German; translated by FBI Cryptanalysis-Translation Section; claims V-weapon flying saucer tested in 1944, now in series production; 48–50 m diameter disc with 15–50 outer-rim circular nozzles; central plexiglass sphere holding atom-bomb / instrument payload; range 30–35,000 km; "in Russian hands"; signed "H. SCH."; cites **constructor Riedel** (Klaus Riedel, real German V-2 rocket engineer). Prototype "Nazi-saucer" claim.
- **BSRA / Meade Layne / Max Freedom Long (Aug 1952, San Diego)** — Francis Ohm (member, operates insurance business in same building as FBI office) claims BSRA predicted Tehachapi earthquake; predicted Pacific tidal wave washing away Japanese islands, Hawaii, U.S. West Coast; predicted new Pacific island; claimed saucers are "factual and actual," "in conversation with the men operating the flying saucers," on "peaceful mission," have "disintegrators" to "disintegrate these planes completely in no time flat" if U.S. fires on them. FBI describes Ohm as "perfectly sane, sound individual."
- **Hoffmeyer Jackson MN (Aug 1952)** — steady white light circling farm; shredded tin-foil sample found in pastures in round pile with clover "burned" underneath in triangle.
- **Riley/Stock Paterson NJ photo (Aug 31, 1952)** — civilian photographer Riley + witness Stock allegedly photographed unidentified aerial object. FBI credit/criminal checks negative; no photo investigation conducted.
- **F. Eekhout The Hague (May 5 / Oct 6, 1952)** — Dutch citizen visits FBI Washington office to discuss "workable flying disc" plans; promises silence; 5 months later writes Hoover from The Hague asking if silence agreement still valid, subject "become active in Europe." Hoover forwards to Air Force.
- **Daniel Lang / New Yorker (Sep 6, 1952)** — full article pasted covering Project Saucer history, Major Boggs interviews, Chiles-Whitted, Kenneth Arnold, Fargo ND light pursuit, Dr. Hynek analysis, Dr. Fitts mass-suggestibility, Langmuir thunderstorm-paper conclusion, Washington flap (Jul 20 + Jul 26, 1952). FBI counter-intel investigation: Ruppelt/Chop debunk FBI-saucer-investigation claims; Lang gathered material ~2 years ago from newspapers.
- **Albert K. Bender / IFSB / Space Review (Jan 1953)** — Robert D. Wolf (Civil Defense Director, Johnson County IN) writes Hoover asking if IFSB "cleared" or "subversive." IFSB officers: Bender (President/Editor), Max Krengel (VP/Treasurer), Allan C. Rievman (Secretary). British branch Bristol. Contents: Mayagüez PR; Norway/Sweden; Korea front; Gaillac France "angel hair"; Oloron France. Reverend Daw Charleston WV landing with "two small men in red." Bender will dissolve IFSB 1953 after "three men in black."
- **Hoover → Wolf (Jan 27, 1953)** — "No references can be located in Bureau files on I.F.S.B."
- **Montana white object (Sep 19, 1952)** — streaking 100 miles; no investigation.
- **Desvergers West Palm Beach cap burn (Aug 1952)** — scoutmaster claims MBB object hovered, shot "red blob." FBI lab: burns present but inconsistencies (non-uniform singeing, fold under insignia shows no singeing — cap not being worn).
- **Bureau Bulletin enforcement (Aug 18, 1952)** — field offices not referring flying-disc complaints to OSI per Bulletin #57 / SAC Letter #38. New SAC Letter issued directing prompt referral.
- **Air Force FAQ pasted** — 1500 reports, ~20% unexplained; "No orders to fire on unidentified aerial phenomena"; future plans: diffraction grating cameras, Schmidt telescopes.

### Cross-page updates

- None this round; new entity pages created for Bender, IFSB, BSRA, Lang, Ruppelt, Reece, and Eekhout.

### Index changes

- "Section 7" line updated from stub to full content gloss.
- 7 new entity pages added under "Individuals" and "Organizations": Bender, IFSB, BSRA, Lang, Ruppelt, Reece, Eekhout.

### Coverage changes

- `62-hq-83894-section-7.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-09] ingest | 62-HQ-83894 Section 5 (209 pages, full read)
- **Knoxville / Oak Ridge radar tracking (March 1–8, 1950)** — Stuart E. Adcock (radio station WROL) used surplus Army APN-7 radar to detect objects at 40,000–100,000 ft over Oak Ridge. Multi-agency response: AEC Security, OSI, CIA (technician dispatched), Naval Reserve, 3rd Army, USAF NEPA. Colonel C. D. Gasser (NEPA engineering officer) and Captain Robert Cross examined Adcock's equipment; found him "technically incorrect" and with "some degree of inebriation." Despite this, Gasser stated 100,000 ft flight was "absolutely possible." Adcock's reliability ultimately deemed "extremely dubious" after he couldn't be found. AEC's procedural critique: "an impressive lack of any agency actually taking the responsibility."
- **U.S. News & World Report (April 7, 1950)** — full-page "FLYING SAUCERS — THE REAL STORY: U.S. BUILT FIRST ONE IN 1942." Comprehensive article arguing saucers are real Navy VTOL aircraft (Zimmerman NACA 1942 model → Chance-Vought XF5U Flying Flapjack). Current saucers use variable-direction jet nozzles, speed 400–500+ mph, vertical takeoff, hover. White Sands ground instruments measured saucer at "well over the speed of sound." Bureau filed without comment.
- **Peter Cameron Jones LA mountains (Aug 1947)** — full letter preserved via Winchell/Cuneo (July 1949). Silver/green top-shaped object with windows and "life within"; knocked him down when departing. LA SAC negative (no record of Jones at given address).
- **George Koehler Denver lectures (Jan–Mar 1950)** — claimed Mojave Desert disc with three-foot-tall dead occupants; Newton Oil Co. connection; *True Magazine* article; visited by Don Shoemaker. Denver OSI considered him "probable mental case."
- **Michael Halfery Mars photographs (May 1950, New Orleans)** — $1.00 purchase of saucer + Mars-man photos; CID forwarded to FBI. Intelligence Branch rated source "usually reliable," information "probably true."
- **Lewis Ward "Russian drawings" (Apr 1950, Yuba City)** — claimed contact with "Ubalsky" and Russian saucer blueprints. Later assessed as "abnormal mentally" by FBI interviewer.
- **Al Hixenbourg Louisville film (Jun 28, 1950)** — Times photographer's 16mm saucer film; debunked by fellow photographers as match-trick; Winchell comment received.
- **Alice TX disc hoax (Jul 4, 1950)** — welded airplane-wing elliptical object, "DO NOT TOUCH" markings, serial X-147A.
- **Martin "Danse Macabre" letter (Jul 25, 1950)** — Hughes-funded one-way bombing saucer by German WWI ace; Chicago indices negative.
- **Walter D. Jones Toronto sighting (Jul 19, 1950)** — hazy light object circling farm 35 min; Toronto Soviet-Friendship Council treasurer.
- **DCOMSA comprehensive analysis** — five-theory debunking; 75% known causes; mass hysteria attribution.
- **Donald Keyhoe book note** — FBI Crime Records Section review of "The Flying Saucers are Real"; false FBI references noted.

### Cross-page updates

- None this round; new entity pages created for Hottel, Adcock, Gasser, Jones, Koehler, Halfery, Hixenbourg, and Keyhoe.

### Index changes

- "Section 5" line updated from stub to full content gloss.
- 8 new entity pages added under "Individuals": Hottel, Adcock, Gasser, Jones, Koehler, Halfery, Hixenbourg, Keyhoe.

### Coverage changes

- `62-hq-83894-section-5.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-09] ingest | 62-HQ-83894 Section 6 (271 pages, full read)

Eighth file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 271)`; now `Coverage: full (271 pages OCR'd)`.

### Key findings

- **Dr. LaPaz 10-point analysis of green fireballs vs. meteors** — Ten significant differences: horizontal path (genuine meteors rarely move horizontally), very low height vs. 40+ miles for meteors, velocity less than meteoric but greater than V-2/jets, no noise (genuine meteors always accompanied by violent noise), brightness variation differs, frequency peaks at 2030 local time (coincides with neither meteor shower max nor meteorite fall max), green color distinguishing feature, lateral movement, penetration to lower altitudes, maneuvers impossible for meteors. Half meteoric; other half (green fireballs + discs) believed to be U.S. guided missiles, or if not, Soviet missiles from Urals (<15 min to NM).
- **Los Alamos conferences (Feb 17, 1949 + Oct 14, 1949)** — Multi-agency: Fourth Army, AFSP, UNM, FBI, AEC, UC, USAF Scientific Advisory Board, Geophysical Research Division AMC, OSI. **"A logical explanation was not proffered... generally concluded that the phenomenon existed and should be studied scientifically."** Continued occurrence near sensitive installations = "cause for concern."
- **Stanfield photograph (Sighting No. 175, Datil NM, Feb 24–25, 1950)** — Holloman AFB Cpl. Lerius B. Stanfield photograph; LaPaz analysis: angular diameter ~1/4 degree, angular velocity >0.5 deg/min. Not moon (too small), not Venus (too large), not star (motion double diurnal rotation). Object genuinely moving.
- **Project Twinkle / Land-Air Inc.** — Geophysical Research Division AMC Cambridge MA contracted Land-Air Inc. Alamogordo NM for scientific study. Vaughn NM observation posts. May 24, 1950: 8–10 objects sighted. 24-hour day-watch as "Project Twinkle." Dr. Mirarchi (Project Engineer) briefed on FBI espionage/sabotage jurisdiction.
- **Oak Ridge radar detections (Dec 1950–Jan 1951)** — 663rd AC&W Squadron McGhee Tyson Airport: Dec 18 (NEPA employees bright circular light over AEC area, Hood/Hribar/Steele/Carss/Gray/Frey); Dec 19 (Miller/Calkins/Coneybear/Mooneyham/Bly circular light with darkening perimeter); Dec 20 (radar "very, very slow" paint, F-82 "perfect intercept orbiting small smoke cloud"); Dec 14 (3-hour radar event, scope photos by Lt. Robinson); Jan 16 (western light through 20× scope: "many peculiar forms with lines, cores, tails"; Clevenger: fits "all flying saucers ever described").
- **LOOK magazine (January 1951)** — Liddel Skyhook balloon explanation in full. "Flying saucers were, and are, undeniably real." 100-ft balloons, 200 mph, up to 19 miles. $40M/year program. Mantell was chasing Skyhook balloon. "Squadrons" = clusters of 20–30 small balloons. "Unheard of until ONR's experiments began."
- **Aaron Hitchens New Haven sighting (Oct 20, 1950)** — Winchester Repeating Arms Chemical Engineer; sphere with steady golden-orange glow; 10× Venus diameter; 400–700 mph; wife/daughter corroborated; "very reliable and sincere."
- **Hoover → LA SAC on Frank Scully (Oct 13, 1950)** — Investigation of alleged communist activities since late 1930s. Follow-up Oct 18 ordered "IMMEDIATELY RESULTS." Answer not in this section.
- **Ladd memo (Oct 9, 1950)** — 3–4 complaints/month; OSI "fails to indicate space ships or missiles from any other planet." No Korean War increase.
- **Philadelphia parachute-disc (Sep 26, 1950)** — Vare Blvd; "soap suds"; officers' report preserved in full.

### Cross-page updates

- None this round; new entity pages created for LaPaz, Liddel, Scully, Land-Air Inc., Mirarchi, and Hitchens.

### Index changes

- "Section 6" line updated from stub to full content gloss.
- 6 new entity pages added under "Individuals" and "Organizations": LaPaz, Liddel, Scully, Land-Air Inc., Mirarchi, Hitchens.

### Coverage changes

- `62-hq-83894-section-6.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-09] ingest | 62-HQ-83894 Section 5 (209 pages, full read)

Fifth file in the post-OCR re-ingestion track, completing the Army incident-summaries trio (1-100, 101-172, 173-233). Previously `Coverage: sampled (pages 1-20 and 100-115 of 209)`; now `Coverage: full (209 pages)`.

### Key findings

- **The complete Mantell incident witness corpus** (Incidents 33 / 33a-33g) — previously absent from the sampled pages — is now in the wiki. Witnesses: T/Sgt Quinton A. Blackwell, 1st Lt Paul I. Orner, PFC Stanley Oliver, Capt. J. F. Duesler Jr., Capt. Cary W. Carter, Col. Guy F. Hix, Capt. Thomas F. Mantell himself, Lt. C. W. Thomas, an anonymous Madisonville KY witness with a **Finch telescope**, and pursuing-flight pilot NG 800. Mantell's verbatim radio transmissions are preserved across multiple witness reports. **AMC file note: "Apparently, Mantell blacked out at 20,000 ft."** — pre-empting the Skyhook hypothesis with an oxygen-deprivation framing. Same-night Venus / comet hypothesis preserved with investigator's parenthetical "(?)".
- **First substantive overseas-theater pilot encounters** in the SIGN corpus: Capt. Peck / Co-pilot Daly **NW of Bethel Alaska 4 August 1947** (saucer-as-large-as-C-54 pursuit; pilot rated "not the imaginative type"); Capt. Griffin / 2nd Officer Polhems **Pan Am Pacific Midway-Oahu 12 September 1947** (intense white light split into two reddish lights at ~1,000 knots, ruling out meteor "for the manner in which it held altitude"); SS *Burgeo* **off Newfoundland 20 July 1947** (4-5 silvery-to-red flashes, faster than tracer bullets).
- **Twin Falls ID multi-formation police mass-witness event (19 August 1947)** — sequence of 1 / 10 / 5-6 / 35-50 objects in triangular formations NE, then groups of 3-5-7 returning SW, witnessed by the Twin Falls Housing Authority executive Mr. Hedstrom + family + neighbors + three Twin Falls PD officers. **Largest pre-1948 mass-formation count in the SIGN corpus.**
- **Pentagon-level scientific officer witness**: Lt Col F. L. Walker Jr. (GSC, Scientific Branch, Research Group, R&DD WDGS) saw a horizontal bright orange glow at Silver Springs OH on 10 August 1947 and ruled out comet hypothesis based on the 70°-arc-vanishing-in-mid-air signature.
- **Polished-nickel hovering disc (R. J. Madden, Pacific Telephone & Telegraph, 25 mi NE of Helena MT, 29 July 1947)** — 3 ft × 3-4 inches thick, vertical 50-100 ft oscillation, then "swooping NE at tremendous speed" and "melted into thin air."
- **Chromium rectangular vanishing-into-smoke-ball (Switzer, Placerville CA, 14 August 1947)** — object engulfed in 10-ft-diameter dark grey smoke and disappeared completely with no falling particles.
- **Case-numbering not strictly chronological** confirmed: Incidents 78-91 contain late-June 1947 events (28-30 June 1947) — pre-Arnold-publicity sightings ingested retroactively as the wave matured.

### Cross-page updates

- [[entities/mantell-incident]] — confidence raised from `medium` to `high`; full witness corpus + verbatim radio transmissions added; the "Mantell blacked out" file note and the same-night Venus / comet astronomer note added; cross-link to [[summaries/62-hq-83894-section-6]] for the Liddel Skyhook hypothesis preserved.

### Index changes

- "Incident Summaries 1-100" line updated from "Muroc cluster, Portland police wave" stub to a full content gloss including the new findings.

### Coverage changes

- `army-incident-summaries-1-100.md`: `sampled` → `full`; `confidence: medium` → `high`.

## [2026-05-09] ingest | 62-HQ-83894 Section 2 — full re-ingest from OCR

Sixth file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 194)`; now `Coverage: full (194 pages)`.

### Key findings

- **The complete Kenneth Arnold packet** is in this section: SA **Frank M. Brown's CIC 4th AF interview report dated 16 July 1947** (Brown died 1 August 1947 in the Kelso WA B-25 crash, so this is one of his final work-products), Arnold's full first-person statement of the 24 June 1947 sighting (eight-day-clock sweep-second-hand timing across the Mt. Rainier → Mt. Adams 50-mile span yielding the canonical ~1,700-mph figure; "geese flying in a diagonal chain-like line" analogy; "mirror bright", "longer than wide, thickness about 1/20th of their width"; consultations with Yakima friend Al Baxter and Pendleton OR ex-Army Air Forces pilot Sonny Robinson), Arnold's autobiographical sketch (born 1915 Minnesota, Olympic fancy-diving trials 1932, Great Western Fire Control Supply 1940), and David N. Johnson's corroborating interview at the Idaho Daily Statesman.
- **Full Rhodes Phoenix photographs chain-of-custody** preserved: Phoenix file 62-213 SA J. Bailey Brower's report documents George Fugate Jr. (A-2 4th AF) calling at the Phoenix office 29 August 1947, Brower's introduction to Rhodes "only as a representative of the United States government" at Fugate's request, Fugate's silence when Rhodes asked whether negatives would be returned, Brower's insistence that Rhodes be advised of Fugate's identity AND that the negatives would not be returned, and Rhodes's 30 August 1947 surrender of the negatives "with the full understanding that they were being given to the Army and that he would not get them back." Phoenix office "did not receive the Bureau teletype" instructing no joint investigation until after the matter had been handled — a documented procedural breach.
- **"Radio Ham" coded message in Newsday** — Mrs. A. G. Sarbanis decoded a 10 July 1947 letter-to-the-editor as a Martian-origin claim ("I AWAIT ATOMIC WAR DISRUPTING ORDER... I [ALSO?] SENT FLYING DIS[CS]... [WILL?] SET UP WORLD ORDER UNDER MARTIANS"); FBI Laboratory confirmed the decoding as substantively correct. Editor Jack Altschul attributed the original to "some local screwball." FBI declined to pursue the FCC amateur-radio cross-walk that the Lab offered.
- **Twin Falls multi-formation event Banister teletype** preserved with verbatim text: "It is believed continued appearance of such objects without official explanation may result in hysteria or panic Twin Falls, Idaho." AAF response on 25 August 1947 denied any research at Twin Falls.
- **Hatfield / Ellison Myrtle Creek OR pilot pursuit (6 August 1947)** — silver sphere ~1,000 mph estimated by flight computer, two consecutive take-offs.
- **A. Courtney Parker (Vermont State Dept of Education)** preserved-in-full 17 September 1947 letter on his Rix Ledges paired-objects sighting.

### Cross-page updates

- [[entities/kenneth-arnold]] — confidence raised from `medium` to `high`; full first-person-statement details, Brown CIC interview verdict, Arnold's biographical sketch, and Brown's 15-days-before-Kelso-crash timeline added.

### Index changes

- "Section 2" line updated from "includes Rhodes Phoenix photos negotiation" stub to a full content gloss.

### Coverage changes

- `62-hq-83894-section-2.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-08] synthesis | Top 10 Disclosure-Relevant Documents

User-prompted curated ranking of the ten primary-source documents in this corpus most relevant to the contemporary (2017–2026) UAP disclosure context. Synthesis at [[syntheses/disclosure-movement-top-10]]. Lists Hunter NASC 1963 memo, DoW Western US 2026 slides + USPER, 2020 Arabian Gulf cluster, FBI HQ 62-HQ-83894 Section 10, Maury Island Affair, Apollo 11 technical debrief, Socorro/Zamora, Borman Gemini-7, FBI 62-HQ-83894 Sub A scrapbook, USAIRA Prague 1955 Czechoslovakia disc — with comparison table, "what's not in the corpus" honest gap-list (Roswell, Tic-Tac, AATIP DIRDs, etc.), and three suggested next-synthesis directions (institutional-handling timeline, abduction-primary-source page anchored on the newly-surfaced Collins 1967 report, recovered-material review). Confidence: `medium` — items are verified but the ranking reflects editorial weighting.



## [2026-05-09] re-ingest | 62-HQ-83894 Section 3 (190 pages, full read)

- Upgraded `62-hq-83894-section-3.md` from `Coverage: sampled (pages 1-20 and 100-120 of 190)` to `Coverage: full (190 pages OCR'd)`.
- **Novel material surfaced (not in sampled pages):**
  - **Muroc Army Air Field sightings (8 July 1947)** — complete witness corpus: Shoop (5-8 metallic objects), McHenry (2 silver discs + 1 circling object), Strapp (ovular object with fin/nob projections, 90 sec duration), Scott (secretary witness), Nauman (2 discs + 1 maneuvering object). All AAF-dismissed as "nothing to be reported."
  - **R.A. Switzer Placerville (14 Aug)** — polished chromium object with unexplained smoke trail; Agent Moon's "no explanation" assessment.
  - **Ward L. Stewart Hamilton Field (29 July)** — two milky-white objects following P-80 at 3-4x speed; UC Berkeley machine shop chief / pilot witness.
  - **Byron B. Savage Oklahoma City (17-21 May)** — flat disc at 10,000-18,000 ft, size of six B-29s, 3x jet speed; "probably atomic" conclusion.
  - **Brummett / Decker Redmond (13 Aug)** — wingless, tailless, "lighter than aluminum" objects at 3x plane speed.
  - **Anchorage Alaska** — two army officers, spherical metallic object traveling against wind.
  - **Danforth IL "instrument"** — old radio loudspeaker parts; no Mogul connection.
  - **4AF McCoy specimen analysis (25 Aug)** — plaster of paris, Baldwin 1910 speaker diaphragm, bakelite coils, Polymet capacitor. All hoax.
  - **Guam Harmon Field (14 Aug)** — three enlisted men, two crescent-shaped objects at 2x fighter speed.
  - **Urie Snake River Canyon (13 Aug)** — sky-blue object at 75 ft with fiery glow; trees "spun around as if in a vacuum."
  - **Palmer's August 5, 1947 letter** — post-crash letter urging Arnold to continue; "nearly forty years" of disk knowledge; "red corpses in your eyeball."
  - **Anonymous caller full transcript** — blow-by-blow descriptions of Winthrop Hotel room 502; "All I am interested in is seeing that this information gets back to New Jersey."
  - **FBI discontinuation memo chain** — Springer's "ash can covers, toilet seats" letter → SAC SF objection ("insulting to the Bureau") → Ladd memo recommending protest → Bureau Bulletin No. 57 (1 Oct 1947) formal discontinuation.
  - **Warden Henry / McHill Ellensburg-Seattle (5 May)** — silver object disintegrating into persistent "pillar of gas."
  - **Philadelphia Aug 6 multi-witness** — Snyder (ex-B-24 pilot), Kelly (retired police), Naddle, Fine — all corroborating bluish-white flaming object.

### Cross-page updates

- None yet — entity pages for Muroc AAF, Harmon Field Guam, Wright Field, Air Defense Command, Byron Savage, Ward Stewart, Muroc witnesses (Shoop, McHenry, Strapp, Scott, Nauman) are candidates for future creation.

### Index changes

- "Section 3" line updated from stub ("Maury Island CIC investigation, Kelso B-25 crash") to full content gloss.

### Coverage changes

- `62-hq-83894-section-3.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.


## [2026-05-09] re-ingest | 62-HQ-83894 Section 4 (214 pages, full read)

- Upgraded `62-hq-83894-section-4.md` from `Coverage: sampled (pages 1-20 and 100-120 of 214)` to `Coverage: full (214 pages OCR'd)`.
- **Novel material surfaced (not in sampled pages):**
  - **Chiles-Whitted Eastern Airlines DC-3 (24 July 1948)** — full Atlanta Journal article: wingless black aircraft, 100-ft fuselage (4x B-29 circumference), two rows of brilliantly lighted square windows, 25-50 ft fiery comet tail, 5,000 ft altitude, "Nary a living soul was seen aboard!" Multiple corroborating witnesses (Atkinson "flying floor lamp", Sellers "cantaloupe ball of fire", etc.).
  - **Madeline Gwynne Merchant letter** — Wichita Falls woman requesting tear sheets; claims worked independently on "aerial missiles" with data to Maj. Sidney Newburger (Los Alamos) and General Thomas T. Handy (Fourth U.S. Army).
  - **Kirtland AFB OSI "OSI-1-96" (31 Jan 1949)** — 22:55Z 30 Jan multi-state sighting (El Paso/Albuquerque/Alamogordo/Roswell/Socorro) by ~30 people of identical object; AEC, AFSWP, 4th Army "perturbed by implications."
  - **Gasser Oak Ridge briefing (24 Jan 1949)** — full details: man-made missiles; Russians 4+ years; North Pole/Ural trajectory; atomic fuel; Czechoslovakia transport collision (only known crash).
  - **Green fireball wave details** — La Paz Starvation Peak: 5200 Å yellow-green, horizontal path, 2 sec, no noise; 10+ analogous incidents + 20 with minor deviations; Los Alamos/Las Vegas/West Texas triangle; copper compound spectrum; 3-12 mi/sec; 9 scientific reasons rule out meteorites.
  - **Camp Hood/Killeen flares (March 6-31, 1949)** — Lt. Frederick Davis March 31: reddish-white basketball-sized object with fire trail at 6,000 ft, 10-15 sec, no sound/odor, telephone static reported.
  - **Parrott Merced CA (4 April 1949)** — Major William H. Parrott (USAF Reserve, 2,200 hrs): solid mass 4-5 ft diameter, dull metal, "clicking" (like home mixer beaters), 90° left turn, dog reacted.
  - **Harrison Fort Smith AR (16 April 1949)** — Special Delivery messenger: brilliant mirror-bright object; Army officer saw it near OKC day before.
  - **Project Grudge formal naming** — G-2 Fourth Army renamed "Unidentified Aircraft" to "Unconventional Aircraft"; investigations named "Project Grudge."
  - **Winchell-Ripley "Japanese saucer"** — OSI Carpenter confirmed through trusted sources: no flying saucers of any kind recovered in US. Checked through individuals not "usual channels" to avoid "stock answer."
  - **Palmer "soul world" theory** — via Kaye Lochrie: discs = explosives from soul warfare; Lochrie visited Hoover to get name cleared from espionage investigation (no record found); agent impressed her as "somewhat mentally unbalanced."
  - **Scranton PA Shaffer house fire** — object struck roof, house burned in <1 hour, fire burned 12 hours after water; ash: Mg/Al/Fe/Ca silicates, sulfur, carbonates; Marquardt (former AEC) submitted to Fire Marshal.
  - **Virginia/Tennessee cigar-streak** — multiple witnesses, black/white cigar with red glow and fire/smoke trail.
  - **G-2 higher authorities Feb 14, 1949** — advised phenomena would ultimately have natural explanation.
  - **G-2 Nov 1, 1948** — Air Force warned "another period of sightings was then imminent."
  - **ADC 4 Feb 1948 letter** — Stratemeyer/Smith: "futile expenditure of military funds and manpower must be avoided."
  - **SAC SF Kimball Feb 12, 1948** — ADC letter contrary to Bulletin 57; SAC SF asked if Bulletin 57 still in effect.
  - **Merced Castle AFB** — Lt. Col. Jacobs Intelligence Officer contacted newspaper; Bremer OSI planning investigation.
  - **Flyin' Saucer toy** — Morrison/Fractions/PIPCO, Southern California Plastic Co., Daytona Beach promotion.
  - **Noack tow-target/kite hoax** — Henry T. Rice positively identified portions.
  - **Cromwell/Lippincott "saucers from Spain"** — Red Cross director told dentist saucers were from Spain, "ascertained by Government in Washington."
  - **Utah Ogden/Logan/Mantua** — Lt. Ron Hatfield, L.N. Jeppson: explosion in air, puffs of smoke, falling silver object.
  - **Bethel AK Peck/Daly** — DC-3 wing-shaped object C-54 size at 1,000 ft, no propeller/exhaust/vapor.
  - **Portland Police multi-witness (11 Sept 1947)** — Officers Adair, Caldwell, Chief Jenkins, Officer Raney; "egg-shaped" object.
  - **Mantell pursuit radio transcript** — "I'm closing in to take a good look... It looks metallic and of tremendous size... That's 360 miles an hour... At 20,000 feet, if I'm no closer, I'll abandon chase."
  - **Dayton Journal-Herald** — "30% weather balloons, 30% conventional, 40% unexplained"; "not a joke, neither are they a cause for alarm."
  - **Fugate "hazy recollection"** — Rhodes interview follow-up; OSI Aldrich requested detailed facts.
  - **Peter Cameron Jones LA prank** — Hoover couldn't locate at given address.

### Cross-page updates

- None yet — entity pages for Gasser, La Paz, Chiles, Whitted, Parrott, Harrison, Mantell, Project Grudge, Kirtland AFB, Camp Hood are candidates for future creation.

### Index changes

- "Section 4" line updated from stub ("green-fireball wave, Chiles-Whitted") to full content gloss.

### Coverage changes

- `62-hq-83894-section-4.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

