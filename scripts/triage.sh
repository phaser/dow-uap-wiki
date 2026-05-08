#!/usr/bin/env bash
# Triage OCR'd PDFs in raw/ocr/ to flag which contain meaningful new content
# vs redundant carbons. Outputs a per-file table with:
#   pages, OCR'd char count, unique-line ratio (lower = more redundancy),
#   incident-density proxy (counts of date-like patterns and named places).
#
# Reads files from a list given on stdin (one basename per line), or all
# raw/ocr/*.pdf if stdin is empty.
#
# Usage:
#   scripts/triage.sh < sampled.txt
#   ls raw/ocr/*.pdf | xargs -n1 basename | scripts/triage.sh

set -u
cd "$(dirname "$0")/.."
export PATH="/opt/homebrew/bin:/usr/local/bin:$PATH"

OCR_DIR="raw/ocr"
TXT_DIR="raw/ocr/.txt"
mkdir -p "$TXT_DIR"

if [ -t 0 ]; then
  files=$(ls "$OCR_DIR"/*.pdf 2>/dev/null | xargs -n1 basename)
else
  files=$(cat)
fi

printf "%-58s %5s %10s %7s %6s %6s\n" "file" "pages" "chars" "uniq%" "dates" "incid"
printf -- '----------------------------------------------------------------------------------------\n'

while IFS= read -r name; do
  [ -z "$name" ] && continue
  pdf="$OCR_DIR/$name"
  [ -f "$pdf" ] || { printf "%-58s %5s\n" "$name" "MISSING"; continue; }

  txt="$TXT_DIR/${name%.pdf}.txt"
  if [ ! -s "$txt" ]; then
    pdftotext -layout "$pdf" "$txt" 2>/dev/null
  fi

  pages=$(pdfinfo "$pdf" 2>/dev/null | awk '/^Pages:/{print $2}')
  chars=$(wc -c < "$txt" | tr -d ' ')

  total_lines=$(grep -c -v '^[[:space:]]*$' "$txt" 2>/dev/null || echo 0)
  uniq_lines=$(grep -v '^[[:space:]]*$' "$txt" | sort -u | wc -l | tr -d ' ')
  if [ "$total_lines" -gt 0 ]; then
    uniq_pct=$(( uniq_lines * 100 / total_lines ))
  else
    uniq_pct=0
  fi

  # date-like patterns: 1947, 1948, ..., or 19xx, or "May 8, 1947"
  dates=$(grep -E -c -i '(\b19[0-9]{2}\b)' "$txt" 2>/dev/null || echo 0)
  # incident-marker proxy: lines with "Sighting"/"Incident"/"Witness"/"observed"/"flying"
  incid=$(grep -E -c -i '(sighting|incident no|witness|observed|flying disc|object[s]? sighted)' "$txt" 2>/dev/null || echo 0)

  printf "%-58s %5s %10s %6d%% %6s %6s\n" "$name" "${pages:-?}" "$chars" "$uniq_pct" "$dates" "$incid"
done <<< "$files"
