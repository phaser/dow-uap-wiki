#!/usr/bin/env bash
# OCR every PDF in raw/ in place (overwrites the original with the searchable
# version) and writes a per-file plain-text extract to raw/ocr/<name>.txt.
# Idempotent: a file is skipped if its corresponding .txt extract already
# exists and is non-empty (signals the PDF has already been OCR'd).
#
# Usage: scripts/ocr.sh [jobs]
#   jobs: parallel pages per document (default 4)

set -u
cd "$(dirname "$0")/.."
export PATH="/opt/homebrew/bin:/usr/local/bin:$PATH"

JOBS="${1:-4}"
SRC_DIR="raw"
TXT_DIR="raw/ocr"
mkdir -p "$TXT_DIR"

shopt -s nullglob
total=0; done=0; skipped=0; failed=0
files=("$SRC_DIR"/*.pdf)
total=${#files[@]}

started_at=$(date +%s)
echo "[$(date '+%H:%M:%S')] OCR start: $total files, jobs=$JOBS"

for src in "${files[@]}"; do
  name=$(basename "$src")
  txt="$TXT_DIR/${name%.pdf}.txt"

  if [ -s "$txt" ]; then
    skipped=$((skipped+1))
    echo "[$(date '+%H:%M:%S')] skip   $name (already OCR'd; .txt exists)"
    continue
  fi

  tmp="$src.ocr-tmp"
  echo "[$(date '+%H:%M:%S')] ocr    $name ..."
  t0=$(date +%s)
  if ocrmypdf \
       --skip-text \
       --rotate-pages \
       --deskew \
       --jobs "$JOBS" \
       --output-type pdf \
       --optimize 1 \
       --quiet \
       "$src" "$tmp" 2>/dev/null \
     && pdftotext -layout "$tmp" "$txt" \
     && mv "$tmp" "$src"; then
    elapsed=$(( $(date +%s) - t0 ))
    size_mb=$(ls -l "$src" | awk '{printf "%.0f", $5/1048576}')
    done=$((done+1))
    echo "[$(date '+%H:%M:%S')] ok     $name  (${elapsed}s, ${size_mb} MB)"
  else
    rc=$?
    rm -f "$tmp"
    failed=$((failed+1))
    echo "[$(date '+%H:%M:%S')] FAIL   $name  (exit $rc)"
  fi
done

elapsed=$(( $(date +%s) - started_at ))
echo "[$(date '+%H:%M:%S')] OCR done in ${elapsed}s — done:$done skipped:$skipped failed:$failed total:$total"
exit $(( failed > 0 ))
