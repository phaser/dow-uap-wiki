#!/usr/bin/env bash
# OCR every PDF in raw/ to raw/ocr/<same-name>.pdf using ocrmypdf.
# Idempotent: a file is skipped if its OCR output already exists and is non-empty.
# Per-file logs go to raw/ocr/.logs/<name>.log.
#
# Usage: scripts/ocr.sh [jobs]
#   jobs: parallel pages per document (default 4)

set -u
cd "$(dirname "$0")/.."

JOBS="${1:-4}"
SRC_DIR="raw"
OUT_DIR="raw/ocr"
LOG_DIR="raw/ocr/.logs"
mkdir -p "$OUT_DIR" "$LOG_DIR"

shopt -s nullglob
total=0; done=0; skipped=0; failed=0
files=("$SRC_DIR"/*.pdf)
total=${#files[@]}

started_at=$(date +%s)
echo "[$(date '+%H:%M:%S')] OCR start: $total files, jobs=$JOBS" | tee -a "$LOG_DIR/_run.log"

for src in "${files[@]}"; do
  name=$(basename "$src")
  out="$OUT_DIR/$name"
  log="$LOG_DIR/${name%.pdf}.log"

  if [ -s "$out" ]; then
    skipped=$((skipped+1))
    echo "[$(date '+%H:%M:%S')] skip   $name" | tee -a "$LOG_DIR/_run.log"
    continue
  fi

  echo "[$(date '+%H:%M:%S')] ocr    $name ..." | tee -a "$LOG_DIR/_run.log"
  t0=$(date +%s)
  if ocrmypdf \
       --skip-text \
       --rotate-pages \
       --deskew \
       --jobs "$JOBS" \
       --output-type pdf \
       --optimize 1 \
       --quiet \
       "$src" "$out" >>"$log" 2>&1; then
    elapsed=$(( $(date +%s) - t0 ))
    size_mb=$(ls -l "$out" | awk '{printf "%.0f", $5/1048576}')
    done=$((done+1))
    echo "[$(date '+%H:%M:%S')] ok     $name  (${elapsed}s, ${size_mb} MB)" | tee -a "$LOG_DIR/_run.log"
  else
    rc=$?
    failed=$((failed+1))
    echo "[$(date '+%H:%M:%S')] FAIL   $name  (exit $rc — see $log)" | tee -a "$LOG_DIR/_run.log"
  fi
done

elapsed=$(( $(date +%s) - started_at ))
echo "[$(date '+%H:%M:%S')] OCR done in ${elapsed}s — done:$done skipped:$skipped failed:$failed total:$total" | tee -a "$LOG_DIR/_run.log"
exit $(( failed > 0 ))
