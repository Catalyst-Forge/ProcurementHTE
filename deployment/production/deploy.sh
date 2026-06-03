#!/usr/bin/env bash
set -Eeuo pipefail

APP_NAME="${APP_NAME:-procurementhte}"
APP_SERVICE="${APP_SERVICE:-procurementhte-web.service}"
APP_PORT="${APP_PORT:-5100}"
BASE_DIR="${BASE_DIR:-/srv/procurementhte}"
BRANCH="${DEPLOY_BRANCH:-master}"
DOTNET_BIN="${DOTNET_BIN:-/opt/dotnet/dotnet}"
REPO_URL="${REPO_URL:-https://github.com/Catalyst-Forge/ProcurementHTE.git}"

REPO_DIR="$BASE_DIR/repo"
RELEASES_DIR="$BASE_DIR/releases"
CURRENT_LINK="$BASE_DIR/current"
DEPLOYED_SHA_FILE="$BASE_DIR/deployed.sha"
LOCK_FILE="/var/lock/${APP_NAME}-deploy.lock"

log() {
  printf '[%s] %s\n' "$(date -u '+%Y-%m-%dT%H:%M:%SZ')" "$*"
}

health_check() {
  local code
  code="$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:${APP_PORT}/" || true)"
  case "$code" in
    200|301|302|401|403|404) return 0 ;;
    *) return 1 ;;
  esac
}

exec 9>"$LOCK_FILE"
if ! flock -n 9; then
  log "Another deployment is already running."
  exit 0
fi

if [ ! -x "$DOTNET_BIN" ]; then
  log "dotnet not found at $DOTNET_BIN"
  exit 1
fi

mkdir -p "$BASE_DIR" "$RELEASES_DIR"

if [ ! -d "$REPO_DIR/.git" ]; then
  log "Cloning $REPO_URL ($BRANCH)"
  git clone --branch "$BRANCH" --single-branch "$REPO_URL" "$REPO_DIR"
fi

git -C "$REPO_DIR" remote set-url origin "$REPO_URL"
git -C "$REPO_DIR" fetch --prune origin "$BRANCH"

NEW_SHA="$(git -C "$REPO_DIR" rev-parse "origin/$BRANCH")"
OLD_SHA="$(cat "$DEPLOYED_SHA_FILE" 2>/dev/null || true)"

if [ "${FORCE_DEPLOY:-0}" != "1" ] && [ "$NEW_SHA" = "$OLD_SHA" ]; then
  log "No changes on $BRANCH ($NEW_SHA)."
  exit 0
fi

log "Deploying $BRANCH@$NEW_SHA"
git -C "$REPO_DIR" checkout -B "$BRANCH" "origin/$BRANCH"
git -C "$REPO_DIR" reset --hard "origin/$BRANCH"
git -C "$REPO_DIR" clean -fdx

RELEASE_DIR="$RELEASES_DIR/$(date -u '+%Y%m%d%H%M%S')-$NEW_SHA"
PREVIOUS_TARGET="$(readlink -f "$CURRENT_LINK" 2>/dev/null || true)"

log "Restoring packages"
"$DOTNET_BIN" restore "$REPO_DIR/ProcurementHTE.sln"

log "Publishing release to $RELEASE_DIR"
"$DOTNET_BIN" publish "$REPO_DIR/ProcurementHTE.Web/ProcurementHTE.Web.csproj" \
  --configuration Release \
  --no-restore \
  --output "$RELEASE_DIR"

chown -R procurementhte:procurementhte "$RELEASE_DIR"
ln -sfn "$RELEASE_DIR" "$CURRENT_LINK.tmp"
mv -Tf "$CURRENT_LINK.tmp" "$CURRENT_LINK"

log "Restarting $APP_SERVICE"
systemctl restart "$APP_SERVICE"

for _ in $(seq 1 30); do
  if health_check; then
    echo "$NEW_SHA" > "$DEPLOYED_SHA_FILE"
    chown procurementhte:procurementhte "$DEPLOYED_SHA_FILE"
    log "Deployment healthy."
    find "$RELEASES_DIR" -mindepth 1 -maxdepth 1 -type d | sort -r | tail -n +6 | xargs -r rm -rf
    exit 0
  fi
  sleep 2
done

log "Health check failed."
if [ -n "$PREVIOUS_TARGET" ] && [ -d "$PREVIOUS_TARGET" ]; then
  log "Rolling back to $PREVIOUS_TARGET"
  ln -sfn "$PREVIOUS_TARGET" "$CURRENT_LINK.tmp"
  mv -Tf "$CURRENT_LINK.tmp" "$CURRENT_LINK"
  systemctl restart "$APP_SERVICE" || true
fi

exit 1
