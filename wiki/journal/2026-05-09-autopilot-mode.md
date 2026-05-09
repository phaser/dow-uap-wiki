---
title: "Autopilot Mode — No More Questions"
type: journal
tags: [session-state, autopilot]
created: 2026-05-09
updated: 2026-05-09
sources: []
confidence: high
---

## Rule

**Never ask the user which file to process next.** If there is work to be done, just do it.

When processing large documents: after each file is fully processed, commit and push immediately. Do not batch commits across files.

Continue until all pending work is complete or the user says stop.
