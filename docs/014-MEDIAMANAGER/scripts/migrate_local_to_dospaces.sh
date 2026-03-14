#!/usr/bin/env bash
set -euo pipefail

# Etapa 4 - migración controlada local -> DigitalOcean Spaces.
# Requisitos: aws-cli v2 configurado para endpoint S3 compatible de DO Spaces.

LOCAL_ROOT="${LOCAL_ROOT:-/var/lib/ioda/media}"
DO_ENDPOINT="${DO_ENDPOINT:-https://nyc3.digitaloceanspaces.com}"
DO_BUCKET="${DO_BUCKET:-ioda-media-dev}"
DO_PREFIX="${DO_PREFIX:-core}"
DRY_RUN="${DRY_RUN:-true}"

if [[ ! -d "$LOCAL_ROOT" ]]; then
  echo "ERROR: LOCAL_ROOT does not exist: $LOCAL_ROOT"
  exit 1
fi

echo "Local root : $LOCAL_ROOT"
echo "Endpoint   : $DO_ENDPOINT"
echo "Bucket     : $DO_BUCKET"
echo "Prefix     : $DO_PREFIX"
echo "Dry run    : $DRY_RUN"

AWS_ARGS=(--endpoint-url "$DO_ENDPOINT")
SRC="$LOCAL_ROOT/"
DST="s3://$DO_BUCKET/$DO_PREFIX/"

if [[ "$DRY_RUN" == "true" ]]; then
  echo "[DRY-RUN] aws s3 sync \"$SRC\" \"$DST\" ${AWS_ARGS[*]} --dryrun"
  aws s3 sync "$SRC" "$DST" "${AWS_ARGS[@]}" --dryrun
else
  echo "[APPLY] aws s3 sync \"$SRC\" \"$DST\" ${AWS_ARGS[*]}"
  aws s3 sync "$SRC" "$DST" "${AWS_ARGS[@]}"
fi

echo "DONE"
