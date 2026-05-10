---
title: "Post-OCR Re-ingestion Track — Resume Here"
type: journal
tags: [session-state, re-ingestion, ocr]
created: 2026-05-09
updated: 2026-05-09
sources: []
confidence: high
---

## Purpose

Single document that lets a future session pick up the **post-OCR re-ingestion track** without re-deriving context. If you're starting a new session and the user says "continue with the next sampled file" or similar, **read this first**.

## What the track is

After the corpus was OCR'd in [[log#2026-05-08-ocr-persistent-ocr-pipeline-raw-ocr-committed|this session]], 17 wiki summaries previously flagged `Coverage: sampled` became candidates for upgrade to `Coverage: full`. Each upgrade involves: read the OCR'd text for the un-sampled pages, surface novel material the original sampled summary missed, rewrite the summary with the new content, update cross-page links and index/log, commit, push.

The triage ranking (in `wiki/log.md` under the OCR entry) is the priority order.

## Status (as of 2026-05-09 evening)

**Done — 9 of 17:**

| # | summary file | date range | most consequential novel artifact |
|---|---|---|---|
| 1 | `62-hq-83894-section-10` | 1966-1977 | Carter WH inquiry (Jun 1977), Peyerl Black-Forest saucer claim (Apr 1967), James Collins abduction (Jan 1967), Larry Stephens MIB letter (May 1969), Milwaukee NMCC inquiry (Aug 1974), full Project Blue Book Fact Sheet |
| 2 | `army-incident-summaries-173-233` | Aug-Dec 1948 | Andrews AFB pursuit (Lt. Combs, Nov 1948), Wakkanai-Japan Soviet "Ferret" attribution, Godman AFB Venus case (Aug 1948), MIT cosmic-ray balloon misidentification, first explicit Hynek consultancy reference |
| 3 | `army-incident-summaries-101-172` | Dec 1947–Aug 1948 | Bakersfield burning-aircraft wave, Ashley/Delaware OH 6-witness "mother-of-pearl" cylinder, Selfridge MI disc formations, Rapid City SD 12-object diamond, Scandinavian Peenemünde-direction ghost-rockets continuing into 1948, Lt. Meyers Philippines pilot pursuit |
| 4 | `62-hq-83894-section-1` | Jun-Aug 1947 | Schulgen-FBI cooperation negotiation, Hoover's "Iowa case" remark, Bureau Bulletin No. 42, Davidson/Brown Kelso B-25 crash sequence, Portland police 4 Jul multi-officer event, Mrs. Merchant Santa Fe AEC thread, "Memorandum of Importance" (Meade Layne / etheric-origin treatise) |
| 5 | `army-incident-summaries-1-100` | Jun 1947–Jan 1948 | Complete Mantell witness corpus (Inc 33-33g) with verbatim radio transmissions and "Mantell blacked out at 20,000 ft" file note; Capt. Peck Bethel Alaska pursuit; Pan Am Capt. Griffin Pacific 1,000-knot light-split; Twin Falls 35-50-object police mass-witness; Lt Col Walker GSC Pentagon-scientific witness |
| 6 | `62-hq-83894-section-2` | Jun-Sep 1947 | **Complete Kenneth Arnold first-person statement + Frank M. Brown CIC interview report dated 16 Jul 1947 (Brown died 15 days later in the Kelso B-25 crash)**; full Rhodes Phoenix photo chain-of-custody (Fugate/Brower negotiation); "Radio Ham" Newsday coded-message Martian-origin claim decoded by FBI Lab |
| 7 | `62-hq-83894-section-9` | Nov 1957-Apr 1960 | Post-Sputnik wave (Roach memo), ICARF civilian filter-center network, Oveson / Torrington CT fictitious military unit, Keyhoe / NICAP formal inquiry (Bureau's dismissive file notes), "Bender Affair" / Marck "chronic" classification, 13-year-old Maney "Interplanetary Intelligence," Hayden Hewes Hoover article request, APRG / Seattle fireman Gribble, Perry moon photograph → Eisenhower letter → White House → DOJ → FBI referral chain, NICAP policy follow-up, Wackerbarth memo: zero OSI referrals past 12 months |

**Also done (from earlier sessions, not in journal table):**
| 8 | `62-hq-83894-section-3` | Jun-Aug 1947 | Maury Island CIC investigation, Kelso B-25 crash, Muroc AAF witness corpus (Shoop/McHenry/Strapp/Scott/Nauman), Switzer chromium vanishing object, Stewart Hamilton Field P-80 pursuit, Savage OKC disc, Brummett/Decker Redmond wingless objects, Anchorage sphere, Danforth IL hoax, 4AF McCoy specimen analysis (plaster of paris + bakelite), Guam Harmon Field crescent objects, Schulgen subversive-investigation request, FBI discontinuation via Bureau Bulletin No. 57 |
| 9 | `62-hq-83894-section-4` | Sep 1947-Mar 1949 | Green fireball wave (La Paz Starvation Peak), Kirtland OSI multi-state 30 Jan 1949 sighting, Chiles-Whitted Eastern Airlines DC-3, Camp Hood/Killeen flares, Parrott Merced clicking object, Harrison Fort Smith mirror-bright object, Project Grudge formal naming, Winchell-Ripley debunk, Gasser Oak Ridge briefing, Mantell pursuit radio transcript, Dayton Journal-Herald statistics, ADC "ash can covers" letter, Fugate Rhodes follow-up, Flyin' Saucer toy, Noack tow-target hoax, Scranton PA house fire (Mg/Al/Fe/Ca ash), Virginia/Tennessee cigar-streak, Alexandria LA "saucer seers convention," Mantell pursuit radio transcript, Bethel AK Peck/Daly DC-3 wing |
| 10 | `62-hq-83894-section-5` | Jul 1949-Sep 1950 | Guy Hottel memo (22 Mar 1950) — three recovered saucers in New Mexico, nine 3-foot-tall humanoid occupants in metallic blackout suits; Knoxville/Oak Ridge radar tracking (Adcock, Gasser, CIA technician); U.S. News & World Report Chance-Vought XF5U argument; Peter Cameron Jones LA mountains; George Koehler Denver lectures; Jonathan Caldwell Maryland disks; Glen Sprouce WV yellow rocket; Michael Halfery Mars photographs; Lewis Ward Russian drawings; Al Hixenbourg Louisville film; Alice TX disc hoax; Martin "Danse Macabre" letter; Walter D. Jones Toronto circling light; DCOMSA five-theory debunking; Donald Keyhoe book review |
| 11 | `62-hq-83894-section-6` | Aug 1950-Feb 1951 | Dr. LaPaz 10-point green fireball analysis; Los Alamos conferences (Feb 1949 + Oct 1949); Stanfield photograph Datil NM; Project Twinkle / Land-Air Inc. contract; Oak Ridge radar detections (Dec 1950-Jan 1951); LOOK magazine Liddel Skyhook explanation; Aaron Hitchens New Haven sighting; Hoover → LA SAC on Frank Scully; Philadelphia parachute-disc; Ladd memo OSI assessment |
| 12 | `62-hq-83894-section-7` | Aug 1952-Feb 1953 | Savannnah River AEC sighting; German V-weapon saucer letter; BSRA / Meade Layne / Max Freedom Long; Wm. Hoffmeyer Jackson MN triangle; Riley/Stock Paterson NJ photograph; F. Eekhout The Hague plans; Daniel Lang / New Yorker "Project Saucer"; Albert K. Bender / IFSB / Space Review; Harvel Reece letter; Montana white object; Desvergers West Palm Beach cap burn; Washington National flap; Bureau Bulletin enforcement; Air Force FAQ — 1500 reports, 20% unexplained |
| 13 | `62-hq-83894-section-8` | 1954 | Truman Bethurum / George Hunt Williamson / Valor / Soulcraft / Pelley; L. H. Stringfield / CRIFO Newsletter; Detroit Flying Saucer Club open letter; Frances Swan / Adm. Knowles telepathy case (M-4 from Uranus / L-11 from Hatann); Cincinnati paint incident; Bartkus/McColm moon sighting; Rome Italy cigar object; Carl Keyser Milford OH silver sphere; Charles Yost electric-field letter; Wilber B. Smith Canadian physicist; Jack Gunderman DuBois layout artist; O'Mara Wright-Patt three breakdowns |

**All 17 files complete.**

## Working method (per file)

1. **Baseline**: `cat wiki/summaries/<file>.md` — note Coverage line, what was in the sampled pages.
2. **OCR text**: `wc -l raw/ocr/<basename>.txt` to gauge size; the existing summary's Coverage line tells you which pages were already covered.
3. **Sample read**: `Read` the OCR'd text in chunks for the un-sampled pages. Use `grep` first for case-marker cues (`Incident #`, dates, `Mantell`, `Hottel`, etc.). The OCR-noise variance defeats strict-equality dedup so sampling matters more than completeness.
4. **Identify novel material** vs the existing summary. Don't repeat what's already there.
5. **Write upgrade** — `Write` the summary in full (the upgrade pattern that worked across files 1-6):
   - Update `tags`, `updated`, `confidence` (raise to `high`).
   - Source Metadata: explicit OCR'd-PDF size + `raw/ocr/<name>.txt` text-extract path.
   - Coverage: `full (N pages OCR'd via ocrmypdf; previously-sampled summary upgraded)`.
   - Key Points organized by sub-headings (chronological or thematic; chronological worked best for sections 10 and 1).
   - Notable Incidents: bullet list, dated.
   - Relevant Concepts / Entities (only link to pages that exist OR pages that are clearly worth a future creation — flag the latter rather than orphan-link silently).
   - Open Questions: 4-6 honest items.
6. **Cross-page updates**: if the file's content meaningfully strengthens an existing entity/concept page, update that page too (e.g., the Mantell entity's confidence was raised to `high` after the section 1-100 ingest surfaced the full witness corpus; the Kenneth Arnold entity got the Brown-CIC-interview detail after section 2).
7. **Index update**: edit the one-line gloss for the file in `wiki/index.md`.
8. **Log entry**: append to `wiki/log.md` under the existing chronological structure. Use the format from sections 1-6 for consistency.
9. **Commit**: per-file commit with a substantive multi-paragraph body summarizing what was surfaced.
10. **Push**: only if user has explicitly authorized; otherwise report at end of turn and let user direct.

## Conventions established

- **OCR'd PDF in place**: `raw/<name>.pdf` is the OCR'd version (originals replaced in place per the layout consolidation). Originals are NOT preserved separately — re-runnable from the war.gov manifest via `dow_uap_downloader` + `scripts/ocr.sh`.
- **Text extract**: `raw/ocr/<name>.txt` is committed and is the canonical grep-able form of the corpus.
- **Path references in summaries**: `Raw path: raw/<name>.pdf (gitignored locally); text extract: raw/ocr/<name>.txt`.
- **Schema**: per `CLAUDE.md`. Required summary sections: Source Metadata, Coverage, Key Points, Notable Incidents/Cases, Relevant Concepts, Relevant Entities, Open Questions.
- **Wiki-link format**: relative paths from wiki root. `[[summaries/...]]`, `[[entities/...]]`, `[[concepts/...]]`. Always link to existing pages when mentioned.
- **Confidence**: `high` after a full read with reasonable OCR fidelity; `medium` if OCR noise is substantial; never invent content past what was actually read (per CLAUDE.md Coverage Policy).
- **Commit footer**: `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` (already standard in this repo's git log).
- **Per-file commits, not batched**: commit + push per file is what the user has been signing off; rolls back cleanly if any single file is rejected.

## Cross-cutting open threads worth keeping in mind

These items have been flagged in summary `Open Questions` sections and may be productively chased across multiple files, especially as the FBI HQ section 3-9 and serials 130/164/sub-a are read:

- **Hoover's "Iowa case"** (24 July 1947 marginal note): an early-July 1947 Iowa flying-disc recovery the Army "grabbed" and would not let FBI examine. Watch for the case in subsequent sections.
- **Guy Hottel memo (22 March 1950)** — most-viewed page in the FBI Vault, three saucers + humanoid bodies near a New Mexico AEC installation. Almost certainly lives in section 5 based on date.
- **Davidson/Brown Kelso B-25 crash** (1 August 1947) — the Maury Island sequel; section 3 should contain the FBI investigation file.
- **The "Memorandum of Importance" from 3615 Pine Place San Diego** (Meade Layne / Borderland Sciences attribution suspected but not confirmed). Watch for similar etheric-origin material recurring.
- **The Schulgen-FBI cooperation pattern** — Schulgen's "no classified W/N research projects tied to the flying disks" assurance from 9 July 1947 sets a baseline. Watch for retractions or qualifications.
- **The "Radio Ham" Newsday coded-message tradition** — does it recur in other files?
- **Sub-A (Nash-Fortenberry / Killian / Jung / Swan-Knowles)** — these are some of the most-cited canonical pilot cases of the early-1950s; sub-A is on the priority list and worth elevating its priority if a "what photographic / pilot evidence is preserved" question arises.

## Files / scripts to know

- `scripts/ocr.sh` — full-corpus OCR pass (idempotent, skips files whose `.txt` extract already exists).
- `scripts/triage.sh` — per-file table of pages, OCR'd char count, line- and chunk-uniqueness, date-pattern density, incident-marker counts.
- `wiki/index.md` — master catalog; one-line glosses per summary. Keep updated.
- `wiki/log.md` — append-only chronological log. Keep updated per ingest.
- `wiki/syntheses/disclosure-movement-top-10.md` — synthesis written this session; cite when the user asks "what's most important."

## End-of-turn pattern that's been working

After committing + pushing a file, end the turn with:
1. Commit hash + brief one-line "what got surfaced" summary.
2. Updated progress count ("N of 17 done").
3. Question to user about next file (don't auto-proceed past one file at a time without authorization).

User has been signing off "yes" or directing the next file explicitly.
