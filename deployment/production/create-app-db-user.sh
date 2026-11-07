#!/usr/bin/env bash
# Ganti login SQL aplikasi dari sa ke login khusus yang hanya memegang database ProcurementHTE.
# Password dibuat di server dan tidak pernah ditampilkan.
#
# Jalankan dari laptop, di root repo:
#   ssh catalystforge 'sudo bash -s' < deployment/production/create-app-db-user.sh
#
# Login sa tetap ada untuk backup. db_owner dipilih karena aplikasi menjalankan
# migrasi saat startup. Kalau aplikasi gagal naik, env lama dipulihkan otomatis.
set -Eeuo pipefail

# Semua isi ada di main supaya bash membaca skrip sampai habis sebelum perintah apa pun jalan.
main() {
  local env_file=/etc/procurementhte/procurementhte.env
  local container=procurementhte-mssql
  local db=ProcurementHTE
  local login=procurementhte_app
  local service=procurementhte-web
  local site=https://hte.catalystforge.web.id/Auth/Login

  grep -q '^ConnectionStrings__DefaultConnection=.*User Id=sa;Password=[^;]*;' "$env_file" || {
    echo "Connection string tidak memakai sa, tidak ada yang diubah."
    exit 1
  }

  local sa_password app_password env_backup
  sa_password="$(sed -n 's/^MSSQL_SA_PASSWORD=//p' /opt/procurementhte/.env)"
  app_password="Hte-$(openssl rand -hex 24)-A9"

  sqlcmd_sa() {
    docker exec -i -e SQLCMDPASSWORD="$sa_password" "$container" \
      /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -W -h -1 "$@"
  }

  echo "1/4 Backup DB"
  systemctl start procurementhte-backup.service

  echo "2/4 Membuat login $login"
  # printf adalah builtin, jadi password tidak muncul di daftar proses.
  printf '%s\n' \
    "IF SUSER_ID(N'$login') IS NULL" \
    "  CREATE LOGIN [$login] WITH PASSWORD = N'$app_password', DEFAULT_DATABASE = [$db], CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;" \
    "ELSE" \
    "  ALTER LOGIN [$login] WITH PASSWORD = N'$app_password';" \
    "GO" \
    "USE [$db];" \
    "IF DATABASE_PRINCIPAL_ID(N'$login') IS NULL CREATE USER [$login] FOR LOGIN [$login];" \
    "ALTER ROLE db_owner ADD MEMBER [$login];" \
    "GO" | sqlcmd_sa -i /dev/stdin

  echo "3/4 Mengganti connection string"
  env_backup="$env_file.bak-$(date -u '+%Y%m%d%H%M%S')"
  cp -p "$env_file" "$env_backup"
  LOGIN="$login" APP_PASSWORD="$app_password" python3 - "$env_file" <<'PY'
import os
import re
import sys

path = sys.argv[1]
with open(path) as source:
    text = source.read()

updated, count = re.subn(
    r"(?m)^(ConnectionStrings__DefaultConnection=.*User Id=)sa(;Password=)[^;]*",
    lambda match: match.group(1) + os.environ["LOGIN"] + match.group(2) + os.environ["APP_PASSWORD"],
    text,
)
if count != 1:
    sys.exit("Connection string tidak berubah.")

with open(path, "w") as target:
    target.write(updated)
PY

  echo "4/4 Restart aplikasi dan verifikasi"
  systemctl restart "$service"

  local attempt code sessions
  for attempt in $(seq 1 18); do
    sleep 5
    code="$(curl -s -o /dev/null -w '%{http_code}' "$site" || true)"
    sessions="$(sqlcmd_sa -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE login_name = N'$login';" </dev/null | tr -d '[:space:]')"
    if [ "$code" = "200" ] && [ "${sessions:-0}" -gt 0 ]; then
      echo "Berhasil: aplikasi naik dan terhubung sebagai $login ($sessions sesi)."
      echo "Env lama tersimpan di $env_backup."
      return 0
    fi
  done

  echo "Aplikasi tidak terhubung sebagai $login (http=$code, sesi=$sessions). Memulihkan env lama."
  cp -p "$env_backup" "$env_file"
  systemctl restart "$service"
  exit 1
}

main "$@"
