#!/usr/bin/env bash
# Backup isi volume MinIO (dokumen yang di-upload) ke arsip tar.gz harian.
#
# MinIO tetap berjalan selama backup, jadi file yang berubah saat dibaca hanya
# dicatat sebagai peringatan (tar keluar dengan kode 1), bukan kegagalan.
set -Eeuo pipefail

SRC="${SRC:-/var/lib/docker/volumes/catalystforge_minio_data/_data}"
DEST_DIR="${DEST_DIR:-/var/backups/procurementhte-minio}"
KEEP="${KEEP:-7}"
MIN_FREE_GB="${MIN_FREE_GB:-5}"

log() { printf '[%s] %s\n' "$(date -u '+%Y-%m-%dT%H:%M:%SZ')" "$*"; }
die() { log "GAGAL: $*"; exit 1; }

[ -d "$SRC" ] || die "folder data MinIO tidak ditemukan: $SRC"

mkdir -p "$DEST_DIR"
chmod 700 "$DEST_DIR"

# Disk ini dipakai bersama layanan lain, jadi backup tidak boleh menghabiskannya.
FREE_GB="$(df --output=avail -BG "$DEST_DIR" | tail -1 | tr -dc '0-9')"
SRC_GB="$(du -s -BG "$SRC" | cut -f1 | tr -dc '0-9')"
[ "$FREE_GB" -ge "$((MIN_FREE_GB + SRC_GB))" ] \
  || die "sisa disk ${FREE_GB} GB, butuh $((MIN_FREE_GB + SRC_GB)) GB (data ${SRC_GB} GB + cadangan ${MIN_FREE_GB} GB)"

NAME="minio-$(date -u '+%Y%m%d-%H%M%S').tar.gz"
TMP="$DEST_DIR/.$NAME.tmp"
trap 'rm -f -- "$TMP"' EXIT

log "Backup $SRC -> $NAME"
status=0
tar --create --gzip --file "$TMP" --directory "$SRC" . || status=$?
[ "$status" -le 1 ] || die "tar gagal dengan kode $status"
[ "$status" -eq 0 ] || log "Peringatan: ada file yang berubah saat dibaca"

log "Verifikasi arsip"
tar --list --gzip --file "$TMP" >/dev/null || die "arsip tidak bisa dibaca"

mv -f "$TMP" "$DEST_DIR/$NAME"
chmod 600 "$DEST_DIR/$NAME"
log "Tersimpan: $DEST_DIR/$NAME ($(du -h "$DEST_DIR/$NAME" | cut -f1))"

# Retensi: simpan KEEP arsip terbaru
mapfile -t OLD < <(find "$DEST_DIR" -maxdepth 1 -name 'minio-*.tar.gz' -type f -printf '%T@ %p\n' \
  | sort -rn | tail -n +$((KEEP + 1)) | cut -d' ' -f2-)
if [ "${#OLD[@]}" -gt 0 ]; then
  log "Menghapus ${#OLD[@]} arsip lama (retensi $KEEP)"
  rm -f -- "${OLD[@]}"
fi

log "Selesai."
