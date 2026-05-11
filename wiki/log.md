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

## [2026-05-09] ingest | 62-HQ-83894 Section 8 (217 pages, full read)

Tenth file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 217)`; now `Coverage: full (217 pages OCR'd)`.

### Key findings

- **Contactee movement** — Section 8 captures the FBI's reluctant brushes with the post-1952 contactee genre.
- **Truman Bethurum / George Hunt Williamson / Valor / Soulcraft / Pelley (Cincinnati, June 1954)** — Thomas Eickhoff (Tom's Beauty Salon) reports Bethurum's Taft Auditorium lecture ($2/ticket); Bethurum claims 11 saucer boardings + "ravishing woman commandant" in Nevada desert (Daily Breeze, Dec 31, 1953). Williamson associated with Valor magazine / Soulcraft Press / William Dudley Pelley (onetime fascist Silver Legion founder). Eickhoff visits Lt. Col. O'Mara at Wright-Patterson (June 6, 1954): O'Mara denies saucers exist, calls Keyhoe "a fraud." Eickhoff relays to Keyhoe, who "was going to Washington with his attorneys." Dinner at L. H. Stringfield's home (7017 Britton Ave) with Bethurum, Williamson, Manspeaker, Zimmermann, Eickhoff. Bethurum lecture cancelled (disagreement over tickets).
- **L. H. Stringfield / CRIFO / Newsletter (September 1954)** — Jack Gunderman (DuBois Company layout artist) forwards Stringfield's Newsletter. Key findings: O'Mara "Flying saucers 'do exist'" interview (Sep 21, 1954); three breakdowns (1) controlled saucer from outer space, (2) secret American saucer-like device, (3) unexplained natural phenomena. "Definitely not!" all American devices. Air Force "plans cooperation with public." International censorship: Chilean Naval Mission retracts Orrego Antarctic photos claim.
- **Detroit Flying Saucer Club open letter to President (Sep 22, 1954)** — "government... is perfectly aware of such phenomena, but has adopted a policy of silence and secrecy." "Other countries have already acknowledged these phenomena and have publicly appointed governmental commissions." Calls for "honest and forthright acknowledgment."
- **Frances Swan / Adm. Knowles telepathy case (July 29, 1954)** — John Hutson (Bureau of Aeronautics Security Officer) + Cdr. McQuiston brief FBI. Swan receives telepathic messages from "outer space" since May 27, 1954; writes for 4–5 hours without getting tired. Two ships: M-4 (from Uranus, Commander "Affa") and L-11 (from Hatann, Commander "Ponnar"). 150 mi wide × 200 mi long × 100 mi deep; ~5,000 mother ships (150–200 ft). Contact purpose: protect Earth from atomic/hydrogen bomb destruction ("disrupts magnetic field"); repair "fault lines" in Pacific. Schedule: 6 AM, 12 noon, 6 PM daily. Buzzing in left ear ("annoying and painful"). Husband Guy + daughter Dawlyn hear buzzing but can't receive. Swan says 5,000 "bells" or "flying saucers" would appear over many nations in late August 1954 if physical contact made. ONI declined (no evidence of foreign conversations). Knowles wrote to Senator Margaret Chase Smith → SecDef → Army/Navy/AF; also wrote to President.
- **Cincinnati paint incident (Sep 11, 1954)** — yellow trim on houses stained brown/black; hydrogen sulphide from Millcreek Valley; Kettering Lab samples; Proctor & Gamble doctor; Allgeyer swelling.
- **Bartkus/McColm moon sighting (Rockford IL, Sep 5, 1954)** — amateur astronomers; 6" Cassegranian reflector; spherical object ascending from Mare Humboldtianum; not following true orbit; 12,500 ft. estimated diameter.
- **Rome Italy cigar object (Sep 18, 1954)** — radar 39 min; "parked" in mid-air; exhaust trail; Ciampino Airfield.
- **Carl Keyser Milford OH silver sphere (Jul 23, 1954)** — Civil Defense Col. Smith reports; referred to OSI Wright-Patterson.
- **Charles Yost St. Clair Shores electric-field letter (Nov 14, 1953)** — research kept secret; "connection with the flying saucer" secondary to electric field research; references "research foundation in Western United States."
- **Wilber B. Smith Canadian physicist** — at Knowles' residence with family (unofficial); planned to contact "outer space" Aug 1, 1954 on high frequency.
- **Hoover forwarding (Aug 9, 1954)** — Swan material to OSI, Army G-2, ONI.

### Cross-page updates

- None this round; new entity pages created for Bethurum, Williamson, Pelley, Valor, Soulcraft Press, Stringfield, Swan, Knowles, Bartkus/McColm, Detroit Flying Saucer Club, CRIFO, and Hutson.

### Index changes

- "Section 8" line updated from stub to full content gloss.
- 13 new entity pages added under "Individuals" and "Organizations": Bethurum, Williamson, Pelley, Valor, Soulcraft Press, Stringfield, Swan, Knowles, Bartkus/McColm, Detroit Flying Saucer Club, CRIFO, Hutson.

### Coverage changes

- `62-hq-83894-section-8.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-09] ingest | 62-HQ-83894 Section 7 (205 pages, full read)

Seventh file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 209)`; now `Coverage: full (209 pages OCR'd)`.

### Key findings

- **The Guy Hottel memo (22 March 1950)** — preserved in full. The famous "three saucers, nine bodies" memo. SA Guy Hottel (SAC, Washington) reports that SA R. H. Kurtzman received information from "Kartahowe, Special Investigator, Sex Squad, Metropolitan Police Department" (informant identity obscured). The informant — an Air Force investigator — stated that **three flying saucers had been recovered in New Mexico**: each circular with raised centers, approximately 50 feet in diameter, each occupied by **three 3-foot-tall human-shaped bodies in metallic cloth of fine texture, bandaged in blackout-suit fashion**. The saucers were detected because government high-powered radar in the area interfered with their control mechanisms. This is the **most-viewed page in the FBI Vault**.
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


## [2026-05-10] ingest | 62-HQ-83894 Section 9 (290 pages, full read)

Ninth file in the post-OCR re-ingestion track. Previously `Coverage: sampled (pages 1-20 and 100-120 of 290)`; now `Coverage: full (290 pages OCR'd)`.

### Key findings

- **Post-Sputnik wave (1957-1958)** — Roach memo (12 Nov 1957) documents the Bureau's awareness of a post-Sputnik surge in UFO reports: Vernal UT moon-like object with pulsing blue flame, Levelland TX vehicle interference cases, Kearney NE landing claim, and a Coast Guard cutter sighting over the Gulf of Mexico.
- **ICARF / Robert T. Stone civilian filter-center network** — ICARF aimed to set up UFO Observer Posts worldwide (US, Canada, Europe, Alaska, Australia, Japan, England, Africa, South America). Kansas City FBI indices found **no information identifiable with ROBERT T. STONE or ICARF**. The 1957 Independence MO directory lists only **TERRELL O. STONE**, ice cream maker at Velvet Freeze, at the same address (10717 E. 27th Street Terrace). The Bureau conducted no investigation; KC was designated Office of Origin.
- **Delmaine H. Oveson / Torrington CT — fictitious "Electronics Service Unit 4"** — Oveson, claiming to be "Director of Operations, Electronics Service Unit 4, Roseau, Minnesota," wrote to OSI about a 5 Dec 1957 UFO sighting near Torrington CT. **U.S. Army and Air Force Postal Service confirmed no military units at Roseau, Minnesota.** The case was referred to the FBI.
- **Donald E. Keyhoe / NICAP formal inquiry (22 September 1958)** — Keyhoe requested particulars of the Bureau's UFO role. The FBI's internal memo is notably dismissive: Keyhoe described as "**a flamboyant writer and we have found from previous experience that much of his material is irresponsible**" (1948). Bureau cited a 1941 *Cosmopolitan* article co-written by Keyhoe about Hitler's plan to seize the U.S. Merchant Marine as "completely false." Hoover's draft reply: standard position, interview "not now necessary."
- **The "Bender Affair" / C. Harold Marck, Jr. (7 December 1958)** — Marck claimed Bender formed the International Flying Saucer Bureau in 1952, knew "what the saucers are" in 1953, then was silenced by "**three men in black suits**." FBI classified Marck as a "**chronic**, living in a dream world of flying saucers and stories of the sea." The Bureau obtained Gray Barker's "They Knew Too Much About Flying Saucers" through the Chicago office.
- **13-year-old James Maney / Oklahoma City "Interplanetary Intelligence" (November 1958)** — Maney wrote as "Acting Deputy Director" asking whether FBI agents discouraged saucer investigators. **Hoover replied personally** (2 December 1958). Background check found only a reference to "Jimney" at the same address who collected FBI letterhead stationery.
- **Hayden Hewes / "Research Interplanetary Intelligence" (24 August 1959)** — Hewes requested a 350-word Hoover article for a planned September publication. **Hoover declined** (8 September 1959): "in view of the many similar requests and the pressure of my official duties, I am unable to comply."
- **APRG / Robert Gribble Seattle fireman (1957-1959)** — After an Alabama Lt. Col. claimed APRG's Seattle address belonged to a Russian Consul's chauffeur, the Seattle FBI office found **Robert Gribble, a regularly employed fireman for the City of Seattle**, running APRG as a hobby with 350 members at $5/year. **No Russian Consulate exists in Seattle.**
- **Joseph Perry Grand Blanc MI moon photograph (21 February 1960)** — Professional photographer for 30 years, Perry photographed the moon through a homemade 2,840× telescope (set at 350-400×) and found a **saucer-like object silhouetted against the moon** with "fluorescent glow" and "blue-green radiation trail." Forwarded to OSI Selfridge AFB. Perry wrote directly to **President Eisenhower** (21 March 1960). White House → DOJ → FBI referral chain. Perry received at least 50 letters and calls; NICAP wanted the picture made public.
- **NICAP follow-up (April 1960)** — Richard Hall, Secretary of NICAP, requested confirmation of FBI's UFO policy. Bureau confirmed: no change, does not gather evidence for Air Force, Perry photo not examined by FBI Lab.
- **Wackerbarth memo (5 October 1959)** — confirms past 12 months **zero** flying-saucer complaints referred from FBI to OSI. Reflects the **decline in Bureau-level UFO activity** post-1957.

### Cross-page updates

- None this round; the Keyhoe / NICAP content strengthens [[entities/leonard-stringfield]] and [[entities/donald-keyhoe]] (if created); the Perry content strengthens [[entities/joseph-perry]]; the "Bender Affair" content strengthens [[entities/albert-k-bender]] and [[concepts/men-in-black]].

### Index changes

- "Section 9" line updated from stub ("1957-1960") to full content gloss.

### Coverage changes

- `62-hq-83894-section-9.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-10] ingest | 62-HQ-83894 Serial 130 (126 pages, full read)

Ninth file in the post-OCR re-ingestion track (actually 14th overall). Previously `Coverage: sampled (pages 1-20 and 60-80 of 126)`; now `Coverage: full (126 pages OCR'd)`.

### Key findings

- **Five Newfoundland / Harmon Field sightings** not previously surfaced: (1) 10 July disc with bluish-black trail ~15 mi long, **Robert E. Leidy Kodachrome photographs** of the trail; (2) 11 July Codroy flame-colored disc with cone trail, three witnesses (Legge, Evans, 12-year-old Samms); (3) 20 July SS *Burgeo* four-to-five silvery-to-reddish flashes faster than tracer bullet, Captain Gullage saw same thing 15 July at same location but faster and changing course; (4) 23 July Harmon Field intermittent reddish flashes 3 minutes; (5) Grand Falls Constable LeStrange four round figures phosphorus glow.
- **F. M. Johnson Portland OR letter (20 Aug)** — prospectors in Mt. Adams district saw same objects as Arnold on 24 June: ~1,000 ft, 30 ft diameter, bright top, **clock-hand object in tail**.
- **Dick Rankin Bakersfield (10 June)** — 7,000-hour pilot saw ten objects in V-formation at 8,500 ft, ~350 mph; returned as seven. Resembled **XF5U-1 Flying Flapjack** (only one built, never left Connecticut).
- **Urie / Hawkins Snake River canyon (13 Aug)** — detailed account: sky-blue oblong with red tubular fiery glow, 1,000 mph, atomic energy powered, "S-W-i-s-h" sound.
- **Dan Nelson "Flying Saucer Mystery Solved" (30 July)** — Oklahoma City attorney's theory: saucers are **reflections from car ventilation wing windows**. Multi-page treatise preserved.
- **M. Lenore Gorey Tarzana (6 July)** — saucers + **secondary phenomenon of milky white rays converging on spinning red cornelian fragments** — sent to Director of Military Intelligence via Dr. Hugh L. Dryden.
- **ADC analytical summary** — six physical characteristics derived from reports: metallic surface, blue-brown haze trail, circular/elliptical flat-bottom domed-top C-54 size, two rear tabs, 3-9 in formation above 300 knots, lateral oscillation. Conclusion: "**Something is really flying around.**"

### Index changes

- "Serial 130" line updated from stub to full content gloss.

### Coverage changes

- `62-hq-83894-serial-130.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-10] ingest | 62-HQ-83894 Serial 164 (137 pages, full read)

Previously `Coverage: sampled (pages 1-20 and 60-80 of 137)`. The memo body was already read in full; the un-sampled pages (81-137) are **redundant carbons and onward-distribution endorsements** — identical copies of the memo distributed to Major Air Commands, Air Attachés, and inter-agency addressees. No novel content in un-sampled pages. Upgraded to `Coverage: full`.

### Coverage changes

- `62-hq-83894-serial-164.md`: `partial-coverage` → `full`; `confidence: high` (unchanged).

## [2026-05-10] ingest | 62-HQ-83894 Sub A (124 pages, full read)

Previously `Coverage: sampled (pages 1-20 and 60-80 of 124)`. Now `Coverage: full (124 pages OCR'd)`.

### Key findings

- **Quantico Marine Base red lights (22 nights, Dec 1953)** — Pfc. Norman Viets first sighting 9:05 pm Dec 30 at Tank Park; light ~1.5 ft diameter, 40-15 mph, followed tree line, went straight down then straight up. At least **30 other Marines including half a dozen officers** saw it. On one occasion, sentries reported **three lights at once**. Maj. D. D. Pomerleau (provost marshal) admitted characteristics unexpected on an airliner. When Viets saw it come up, he "grabbed a butcher knife and headed for the tank shed."
- **Keyhoe / Chop / Utah film controversy (Jan 1954)** — Albert M. Chop (former AF civilian saucer expert, now Douglas Aircraft) published letter on Keyhoe's book jacket stating AF is "aware" saucers may be from another planet. AF denied Chop was speaking for them. Keyhoe: "If any official ... says that it did not rule out birds, known aircraft or conventional objects as the cause of those objects, I will call him a liar to his face." Brig. Gen. Sory Smith: "We do not know enough about it to deny that flying saucers exist. Conversely, we have no proof that they do exist."
- **Canada Flying Saucer Observatory (Nov 1952)** — Wilbert B. Smith announces 60% probability saucers are "alien vehicles." Station at Shirley Bay, Ottawa River. Equipment: ionospheric reactor, gamma ray detector, gravimeter.
- **G. Klein Nazi saucer story** — Former German secret weapons expert claims saucers are "top secret weapons of the USA and Russia," prototypes built in Germany during war. Walter Schriever heads U.S. saucer development.
- **G. Tilghman Richards South Kensington (Jul 1950)** — Senior research lecturer studied all saucer reports; concludes "disc-type aircraft." Traces annular monoplane design history (Zimmermann → Chance Vought → Navy secrecy).
- **England Devon/Chard (1950)** — Multiple witnesses from 60+ miles apart: "no noise, trail of fire." G. Tilghman Richards says "photographs of disc-type aircraft."
- **Air Force UFO fact sheet** — 80% explained; photographs "worthless" — "Innumerable objects, from ashtrays to wash basins, have been photographed while sailing through the air." No evidence of foreign or extraterrestrial observation.
- **Reinhold Schmidt follow-up** — State Penitentiary records showed a man of the same name served a term for embezzlement in the 1930's. Sheriff Warrick "convinced [he] saw nothing." Wright-Patt official: 5,700 reports investigated 1947-1957; not a single landing impression, footprint, saucer, or "little green man" found.

### Index changes

- "Sub A" line updated from stub to full content gloss.

### Coverage changes

- `62-hq-83894-sub-a.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-10] ingest | Box 186 / 319.1 Flying Discs 1949 (143 pages, full read)

Previously `Coverage: sampled (pages 1-18 of 143)`. Now `Coverage: full (143 pages OCR'd)`.

### Key findings

- **Mountain Home Idaho delta-wing case (24 Jul 1949)** — Harry Clark (Airport Manager, Ritchie Field) saw **7 delta-wing objects in V formation**, **larger than F-51**, darker than aluminum, **no markings**, no sound, no exhaust, 600+ mph, **dark circular bulge on underside** (where pilot normally sits), **outer wing surfaces appeared to move slightly**. 180° turn without banking. Captain John S. Batie vouches for Clark's reliability.
- **Medford OR multiple witnesses (8 Aug 1949)** — AACS Air/Ground operators + CAA tower operators + CA Range Station communicators saw **shiny objects with wings**, formation-break-reform pattern, speed varying from slow to very fast, visible to unaided eye when reflecting sun.
- **Captain Thrush NW Airlines (30 Jul 1949)** — object **dropping flares** over Portland; Portland Tower instructed other aircraft to hold; B-29 pilot **denied dropping any flares**. Thrush attempted 210 mph intercept — object "pulled away quite easily."
- **Kuhl/Goodrich in-flight (22 Sep 1949)** — cylindrical silvery object with **flame 2x object length**, 43°40′N 74°55′W (Adirondacks region).
- **Olathe/Kansas City (6 Jan 1950)** — 2 spherical objects hovered motionless 10-15 min over Olathe at 7-8,000 ft.
- **RB-29 "intentional jamming" claim (2 May 1949)** — experienced radar operator on RF-21824 noticed interference pulses 10 miles apart, "believes what was picked up was intentional jamming."
- **Seattle 143rd AGACW (22 Aug 1949)** — three NCO controllers saw aluminum disc, **"no resemblance to any aircraft they had ever seen before."**

### Index changes

- "Box 186" line updated from stub to full content gloss.

### Coverage changes

- `box186-319.1-flying-discs-1949.md`: `partial-coverage` → `full`; `confidence: medium` → `high`.

## [2026-05-10] ingest complete | All 17 files re-ingested with full coverage

### Summary of the re-ingestion effort

Started with 17 files that had been previously sampled (pages 1-20 and 60-80 of each). Completed full re-ingestion of all 17 files with `Coverage: full` and upgraded `confidence: high`.

### Files re-ingested in this session

1. **Serial 130** (126 pages) — Five Newfoundland sightings not previously surfaced (Harmon Field disc with Leidy Kodachrome, Codroy flame-colored disc, SS Burgeo flashes, Grand Falls Constable LeStrange); F. M. Johnson Mt. Adams (same as Arnold); Dick Rankin Bakersfield 10 objects V-formation; Urie/Hawkins Snake River canyon; Dan Nelson "Flying Saucer Mystery Solved" (car ventilation-wing reflections); M. Lenore Gorey Tarzana saucers + milky-white-rays-with-spinning-cornelian-fragments secondary phenomenon; ADC analytical summary: "Something is really flying around."

2. **Serial 164** (137 pages) — Memo body already fully captured; un-sampled pages confirmed redundant carbons/distribution endorsements.

3. **Sub A** (124 pages) — Quantico Marine Base 22 nights red lights (30+ Marines, Pfc. Viets "grabbed a butcher knife"); Keyhoe/Chop Utah film controversy; Canada Flying Saucer Observatory; G. Klein Nazi saucer story; G. Tilghman Richards South Kensington disc-aircraft analysis; England Devon/Chard multiple witnesses; Air Force UFO fact sheet (80% explained, photographs "worthless"); Reinhold Schmidt embezzlement record surfaced.

4. **Box 186** (143 pages) — Mountain Home Idaho delta-wing objects with dark circular bulge underside (7 objects, no sound, no exhaust, outer wing surfaces appeared to move); Medford OR AACS/CAA multiple witnesses; Captain Thrush NW Airlines object dropping flares; Kuhl/Goodrich in-flight cylindrical object with flame 2x object length; RB-29 "intentional jamming" claim; Olathe/Kansas City 2 spherical objects hovered 10-15 min.

### Total files in the corpus

17 files total — all now have `Coverage: full` and `confidence: high`.

## [2026-05-11] synthesis | Top 10 Disclosure-Relevant Documents — re-ranked

User asked whether the Top 10 ranking (originally written 2026-05-08) had changed after the full post-OCR re-ingestion of all 17 sampled files. **Yes — materially.** The original was written mid-re-ingestion when the Hottel memo, Los Alamos conferences, and Oak Ridge radar detections were only "honourable mentions."

### What changed

- **#1 Guy Hottel memo** ("Three saucers, nine bodies") — was not on the radar; now the top-ranked document because it is the most-viewed page in the FBI Vault and the direct primary-source anchor for the crash-retrieval narrative.
- **#2 Los Alamos conferences** (Feb + Oct 1949) — was an "honourable mention"; now #2 because the multi-agency conclusion "logical explanation not proffered" is one of the strongest scientific-establishment acknowledgments in the UAP record.
- **#3 Oak Ridge radar over nuclear facilities** — was an "honourable mention"; now #3 because hard sensor data over AEC installations with F-82 intercepts is among the highest-weight evidence in the corpus.
- **#4 O'Mara "do exist" (Section 8)** — was an "honourable mention"; now #4 because Lt. Col. O'Mara (Deputy Commander, Wright-Patt Intelligence) saying "Flying saucers 'do exist'" is a rare high-level military acknowledgment.
- **#5 Green fireball wave** — was an "honourable mention"; now #5 because La Paz's Starvation Peak analysis with 10+ incidents + 20 deviations over a nuclear installation is the most sustained scientifically-documented fireball wave.
- **#10 Hoover's "Iowa case" marginal note** — was not ranked; now #10 because it is the foundational primary-source statement for the "they recovered something and buried it" narrative, from Hoover himself.
- **Dropped from top 10:** 1955 Czechoslovakia attaché report (too narrow), Hunter NASC memo dropped to #6 (still strong but below the Hottel memo), DoW Western US 2026 slides dropped to honourable mentions (sensor data only, no physical evidence).

Full ranking and justifications at [[syntheses/disclosure-movement-top-10]]. Confidence: `high` — all underlying documents are now fully read.

## [2026-05-11] synthesis | Ten Arguments for Non-Human Intelligence

User requested a structured argument for the proposition that UFOs, aliens, and interdimensional beings are real, drawn from this corpus and broader knowledge. Created at [[syntheses/ten-arguments-non-human-intelligence]].

### Structure

Ten arguments, each grounded in primary-source documents:

1. **Hottel memo** — Recovered craft + non-human occupants (FBI Vault's most-viewed page)
2. **Los Alamos conferences** — Scientific establishment: "logical explanation not proffered"
3. **Oak Ridge radar** — Hard sensor data over AEC installations, F-82 intercepts
4. **O'Mara "do exist"** — Wright-Patt Intelligence Deputy Commander: "Definitely not [American devices]"
5. **Green fireball wave** — 30+ incidents, spectral analysis, geographic confinement to nuclear triangle
6. **Socorro / Zamora** — Cleanest physical-evidence case: landing trace, Blue Book "unidentified"
7. **Pilot witness corpus** — Hundreds of credentialed observers across 1947-1949 (Powell, Mantell, Miller, etc.)
8. **Hoover's "Iowa case"** — "The Army grabbed it and would not let us have it" — coverup narrative anchor
9. **Contactee movement convergence** — Independent claimants with consistent cross-cultural patterns (Bethurum, Swan, Stringfield)
10. **JFK timing / institutional response** — FBI escalation pattern suggests recognition of something sensitive

### Honesty caveats

- Roswell, Tic-Tac, AATIP, Grusch testimony — **not in this corpus**.
- Corpus is strongest on 1947-1970 FBI/USAF institutional record and 2020-2026 DoW/AARO modern military record.
- The 1980s–early-2000s middle period is thin.
- Confidence: `medium` — the corpus provides strong documentary evidence for each claim, but the inference to non-human intelligence is interpretive.

## [2026-05-11] ingest | 2 new files from manifest check

Checked war.gov manifest for new material. Found **2 genuinely new files** (3rd candidate `18_100754_vol_2` and `59_64634` had special characters in filenames that the downloader couldn't parse from CSV):

### Serial 153 — R. Presley Oak Ridge "Flying Saucer" Photographs (July 1947)

- **Knoxville File #65-11**, classified "INTERNAL SECURITY."
- 24-year-old R. Presley at 218 Illinois Avenue, Oak Ridge, TN took a photo of the mountain that developed showing a "flying saucer."
- Presley quote: "Now don't start trying to explain it off. Just go ahead and say what it looks like. Sure. Sure. You're right. It is a Flying Saucer."
- Filed under "Internal Security" rather than standard UFO investigation — notable given Oak Ridge's role as the Manhattan Project's uranium production site.
- No follow-up investigation preserved in this serial.
- Summary page: [[summaries/62-hq-83894-serial-153]].

### D20 filename correction

- Manifest corrected `dow-uap-d20-mission-report-southern-united-states-2020.pdf` → `dow-uap-d20-mission-report-southern-united-states-2023.pdf`.
- Content: 77 EFS F-16 DCA mission from Prince Sultan Air Base, March 20, 2023, over Syria. "MULTIPLE POSS UAPS" at 2302Z.
- Same incident as existing D20 summary; only filename was corrected (2020 → 2023).
- SOURCES.md updated to reflect corrected filename.

### Files not found

- `18_100754_general_1946-7_vol_2.pdf` — AMC flying disc concern memos (companion to vol 1 we have). Filename has spaces; downloader couldn't parse from CSV.
- `59_64634_711.5612[7-2852` — State Dept memo on "increased UFO reports" (July 18, 1952). Filename has brackets/special chars; downloader couldn't parse.
- These would need manual download if desired.
