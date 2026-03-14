#!/usr/bin/env bash
set -euo pipefail

# Etapa 4 - smoke de carga/resiliencia para MediaManager (API level)
# Requisitos: curl, jq

IDENTITY_BASE="${IDENTITY_BASE:-http://localhost:5002}"
CORE_BASE="${CORE_BASE:-http://localhost:5001}"
EMAIL="${EMAIL:-josue.guol@gmail.com}"
PASSWORD="${PASSWORD:-q1w2e3r4}"
CONCURRENCY="${CONCURRENCY:-8}"
REQUESTS="${REQUESTS:-80}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-20}"

echo "[1/5] Login..."
TOKEN="$(curl -sS -X POST "$IDENTITY_BASE/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" | jq -r '.accessToken')"

if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  echo "ERROR: could not get access token"
  exit 1
fi

echo "[2/5] Resolver contexto..."
PROJECT_ID="$(curl -sS "$CORE_BASE/api/projects" -H "Authorization: Bearer $TOKEN" | jq -r '.items[0].id')"
if [[ -z "$PROJECT_ID" || "$PROJECT_ID" == "null" ]]; then
  echo "ERROR: no project found"
  exit 1
fi

MEDIA_JSON="$(curl -sS "$CORE_BASE/api/projects/$PROJECT_ID/media?page=1&pageSize=1" -H "Authorization: Bearer $TOKEN")"
MEDIA_ID="$(echo "$MEDIA_JSON" | jq -r '.items[0].id')"

LIST_URL="$CORE_BASE/api/projects/$PROJECT_ID/media?page=1&pageSize=20"
FILE_URL=""
if [[ -n "$MEDIA_ID" && "$MEDIA_ID" != "null" ]]; then
  FILE_URL="$CORE_BASE/api/projects/$PROJECT_ID/media/$MEDIA_ID/file"
fi

echo "[3/5] Carga en listado media ($REQUESTS req, concurrency=$CONCURRENCY)..."
TMP1="$(mktemp)"
seq "$REQUESTS" | xargs -P "$CONCURRENCY" -I{} sh -c \
  "curl -s -o /dev/null -w '%{http_code}\n' --max-time $TIMEOUT_SECONDS -H 'Authorization: Bearer $TOKEN' '$LIST_URL'" > "$TMP1"

OK_2XX="$(grep -Ec '^2' "$TMP1" || true)"
ERR_5XX="$(grep -Ec '^5' "$TMP1" || true)"
ERR_OTHER="$(grep -Evc '^(2|5)' "$TMP1" || true)"
echo "LIST -> 2xx=$OK_2XX 5xx=$ERR_5XX other=$ERR_OTHER"

if [[ -n "$FILE_URL" ]]; then
  echo "[4/5] Carga en file delivery ($REQUESTS req, concurrency=$CONCURRENCY)..."
  TMP2="$(mktemp)"
  seq "$REQUESTS" | xargs -P "$CONCURRENCY" -I{} sh -c \
    "curl -s -o /dev/null -w '%{http_code}\n' --max-time $TIMEOUT_SECONDS '$FILE_URL'" > "$TMP2"
  OK_FILE="$(grep -Ec '^2' "$TMP2" || true)"
  ERR_FILE_5XX="$(grep -Ec '^5' "$TMP2" || true)"
  ERR_FILE_OTHER="$(grep -Evc '^(2|5)' "$TMP2" || true)"
  echo "FILE -> 2xx=$OK_FILE 5xx=$ERR_FILE_5XX other=$ERR_FILE_OTHER"
  rm -f "$TMP2"
else
  echo "[4/5] Saltado file delivery (no media disponible)"
fi

echo "[5/5] Resumen"
echo "- ProjectId: $PROJECT_ID"
echo "- Requests per scenario: $REQUESTS"
echo "- Concurrency: $CONCURRENCY"
echo "- Timeouts: ${TIMEOUT_SECONDS}s"

rm -f "$TMP1"
echo "DONE"
