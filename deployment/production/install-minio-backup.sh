#!/usr/bin/env bash
# Pasang backup harian MinIO di VPS, jalankan sekali, lalu buktikan arsipnya bisa dipulihkan.
#
# Jalankan dari laptop, di root repo:
#   deployment/production/install-minio-backup.sh
set -Eeuo pipefail

HOST="${HOST:-catalystforge}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNIT=procurementhte-minio-backup

ssh "$HOST" "sudo tee /usr/local/bin/$UNIT.sh >/dev/null && sudo chmod 755 /usr/local/bin/$UNIT.sh" < "$HERE/$UNIT.sh"
ssh "$HOST" "sudo tee /etc/systemd/system/$UNIT.service >/dev/null" < "$HERE/$UNIT.service"
ssh "$HOST" "sudo tee /etc/systemd/system/$UNIT.timer >/dev/null" < "$HERE/$UNIT.timer"

ssh "$HOST" 'sudo bash -s' <<'REMOTE'
set -Eeuo pipefail
main() {
  local unit=procurementhte-minio-backup
  local src=/var/lib/docker/volumes/catalystforge_minio_data/_data
  local dir=/var/backups/procurementhte-minio

  systemctl daemon-reload
  systemctl enable --now "$unit.timer"
  systemctl start "$unit.service"

  local latest work
  latest="$(ls -t "$dir"/minio-*.tar.gz | head -1)"
  work="$(mktemp -d /tmp/minio-verify.XXXXXX)"
  tar --extract --gzip --file "$latest" --directory "$work"

  echo "Arsip: $latest ($(du -h "$latest" | cut -f1))"
  echo "File sumber: $(find "$src" -type f | wc -l), file dipulihkan: $(find "$work" -type f | wc -l)"
  if diff -r "$src" "$work" >/dev/null; then echo "Isi identik."; else echo "Ada file yang berbeda, cek manual."; fi

  rm -rf -- "$work"
  systemctl list-timers --all --no-pager | grep "$unit" || true
}
main
REMOTE
