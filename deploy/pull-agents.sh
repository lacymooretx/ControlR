#!/usr/bin/env bash
#
# pull-agents.sh — Pull signed agent binaries from the latest successful
# GitHub Actions build and deploy them into the running ControlR container.
#
# Designed to run as a cron job on the production server.
#
# Requirements:
#   - curl, jq, unzip
#   - A GitHub PAT with repo/actions:read scope in /root/.secrets/.env as GITHUB_TOKEN
#   - Docker access to the "controlr" container
#
# Usage:
#   source /root/.secrets/.env && /opt/docker/controlr/deploy/pull-agents.sh
#

set -euo pipefail

# ── Configuration ──────────────────────────────────────────────────────────────
REPO="lacymooretx/controlr"
WORKFLOW_FILE="build.yml"
CONTAINER="controlr"
STATE_FILE="/opt/docker/controlr/deploy/.last-deployed-run"
LOG_FILE="/opt/docker/controlr/deploy/pull-agents.log"
DOWNLOADS_BASE="/app/wwwroot/downloads"

# Artifact name → container path mapping
declare -A ARTIFACT_MAP=(
  ["Agent-win-x86"]="win-x86/ControlR.Agent.exe"
  ["Agent-win-x64"]="win-x64/ControlR.Agent.exe"
  ["Agent-linux-x64"]="linux-x64/ControlR.Agent"
  ["Agent-macOS-ARM64"]="osx-arm64/ControlR.Agent"
  ["Agent-macOS-x64"]="osx-x64/ControlR.Agent"
)

# ── Helpers ────────────────────────────────────────────────────────────────────
log() {
  local ts
  ts=$(date '+%Y-%m-%d %H:%M:%S')
  echo "[$ts] $*" | tee -a "$LOG_FILE"
}

gh_api() {
  local endpoint="$1"
  curl -sf \
    -H "Authorization: Bearer ${GITHUB_TOKEN}" \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "https://api.github.com${endpoint}"
}

gh_download() {
  local url="$1" dest="$2"
  curl -sfL \
    -H "Authorization: Bearer ${GITHUB_TOKEN}" \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    -o "$dest" \
    "$url"
}

# ── Preflight checks ──────────────────────────────────────────────────────────
if [[ -z "${GITHUB_TOKEN:-}" ]]; then
  log "ERROR: GITHUB_TOKEN not set. Source /root/.secrets/.env first."
  exit 1
fi

if ! docker inspect "$CONTAINER" &>/dev/null; then
  log "ERROR: Container '$CONTAINER' is not running."
  exit 1
fi

# Create state/log dirs
mkdir -p "$(dirname "$STATE_FILE")"

# ── Find latest completed build run with artifacts ─────────────────────────────
log "Checking for new builds with agent artifacts..."

# Look at the last 5 completed runs (not just successful — tests may fail while
# build jobs succeed and produce valid signed artifacts).
RUN_JSON=$(gh_api "/repos/${REPO}/actions/workflows/${WORKFLOW_FILE}/runs?status=completed&per_page=5&branch=main")

# Find the first run that has all expected artifacts
RUN_ID=""
RUN_NAME=""
for i in $(seq 0 4); do
  CANDIDATE_ID=$(echo "$RUN_JSON" | jq -r ".workflow_runs[$i].id // empty")
  [[ -z "$CANDIDATE_ID" ]] && break

  # Check if this run has the expected agent artifacts
  CANDIDATE_ARTIFACTS=$(gh_api "/repos/${REPO}/actions/runs/${CANDIDATE_ID}/artifacts")
  ARTIFACT_COUNT=$(echo "$CANDIDATE_ARTIFACTS" | jq '[.artifacts[] | select(.name | startswith("Agent-")) | select(.expired == false)] | length')

  if [[ "$ARTIFACT_COUNT" -ge 5 ]]; then
    RUN_ID="$CANDIDATE_ID"
    RUN_NAME=$(echo "$RUN_JSON" | jq -r ".workflow_runs[$i].display_title // empty")
    ARTIFACTS_JSON="$CANDIDATE_ARTIFACTS"
    break
  fi
done

if [[ -z "$RUN_ID" ]]; then
  log "No successful build runs found."
  exit 0
fi

# Extract version from run name (format: "Build v1.2.3")
VERSION=$(echo "$RUN_NAME" | sed -n 's/^Build v//p')
if [[ -z "$VERSION" ]]; then
  log "WARNING: Could not parse version from run name: $RUN_NAME"
  VERSION="unknown"
fi

log "Latest successful run: #${RUN_ID} — ${RUN_NAME} (version ${VERSION})"

# Check if already deployed
LAST_DEPLOYED=$(cat "$STATE_FILE" 2>/dev/null || echo "")
if [[ "$LAST_DEPLOYED" == "$RUN_ID" ]]; then
  log "Already deployed run #${RUN_ID}. Nothing to do."
  exit 0
fi

# ── Download artifacts ─────────────────────────────────────────────────────────
# ARTIFACTS_JSON was already fetched during run selection above
TMPDIR=$(mktemp -d)
trap 'rm -rf "$TMPDIR"' EXIT

DEPLOYED_COUNT=0

for ARTIFACT_NAME in "${!ARTIFACT_MAP[@]}"; do
  DEST_PATH="${ARTIFACT_MAP[$ARTIFACT_NAME]}"

  # Find artifact ID
  ARTIFACT_ID=$(echo "$ARTIFACTS_JSON" | jq -r --arg name "$ARTIFACT_NAME" \
    '.artifacts[] | select(.name == $name) | .id // empty')

  if [[ -z "$ARTIFACT_ID" ]]; then
    log "WARNING: Artifact '$ARTIFACT_NAME' not found in run #${RUN_ID}. Skipping."
    continue
  fi

  if [[ $(echo "$ARTIFACTS_JSON" | jq -r --arg name "$ARTIFACT_NAME" \
    '.artifacts[] | select(.name == $name) | .expired') == "true" ]]; then
    log "WARNING: Artifact '$ARTIFACT_NAME' has expired. Skipping."
    continue
  fi

  log "Downloading ${ARTIFACT_NAME} (artifact #${ARTIFACT_ID})..."
  ZIPFILE="${TMPDIR}/${ARTIFACT_NAME}.zip"
  EXTRACT_DIR="${TMPDIR}/${ARTIFACT_NAME}"

  DOWNLOAD_URL=$(echo "$ARTIFACTS_JSON" | jq -r --arg name "$ARTIFACT_NAME" \
    '.artifacts[] | select(.name == $name) | .archive_download_url')

  gh_download "$DOWNLOAD_URL" "$ZIPFILE"
  mkdir -p "$EXTRACT_DIR"
  unzip -o -q "$ZIPFILE" -d "$EXTRACT_DIR"

  # Find the binary inside the extracted zip
  BINARY_NAME=$(basename "$DEST_PATH")
  EXTRACTED_FILE=$(find "$EXTRACT_DIR" -name "$BINARY_NAME" -type f | head -1)

  if [[ -z "$EXTRACTED_FILE" ]]; then
    log "ERROR: Could not find '$BINARY_NAME' inside artifact '$ARTIFACT_NAME'."
    continue
  fi

  # Ensure target directory exists in container
  TARGET_DIR="${DOWNLOADS_BASE}/$(dirname "$DEST_PATH")"
  docker exec -u 0 "$CONTAINER" mkdir -p "$TARGET_DIR"

  # Copy into container
  docker cp "$EXTRACTED_FILE" "${CONTAINER}:${DOWNLOADS_BASE}/${DEST_PATH}"

  # Fix permissions (ensure the app user can read it)
  docker exec -u 0 "$CONTAINER" chmod 644 "${DOWNLOADS_BASE}/${DEST_PATH}"

  log "Deployed: ${ARTIFACT_NAME} → ${DOWNLOADS_BASE}/${DEST_PATH}"
  DEPLOYED_COUNT=$((DEPLOYED_COUNT + 1))
done

# ── Update Version.txt ─────────────────────────────────────────────────────────
if [[ "$DEPLOYED_COUNT" -gt 0 && "$VERSION" != "unknown" ]]; then
  echo -n "$VERSION" | docker exec -i -u 0 "$CONTAINER" tee "${DOWNLOADS_BASE}/Version.txt" >/dev/null
  log "Updated Version.txt to ${VERSION}"
fi

# ── Record state ───────────────────────────────────────────────────────────────
echo "$RUN_ID" > "$STATE_FILE"
log "Deployment complete: ${DEPLOYED_COUNT}/${#ARTIFACT_MAP[@]} artifacts from run #${RUN_ID} (v${VERSION})"
