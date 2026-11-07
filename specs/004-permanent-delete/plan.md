# Implementation Plan: Hapus Permanen

**Branch**: `004-permanent-delete` | **Date**: 2026-11-08 | **Spec**: [spec.md](spec.md)

## Summary

`RecordPurgeService` menyusun rencana per jenis data, yaitu baris yang dihapus, file, dan pemakai yang
menghalangi. Baris log dan penghapusan masuk satu `SaveChanges`, jadi keduanya gagal atau berhasil bersama.
File dihapus setelah commit.

## Constitution Check

| Prinsip | Status |
|---|---|
| I. Batas Lapisan | Lolos. Kontrak di Core, implementasi di Infrastructure, controller hanya memanggil service |
| II. Perilaku Baru Disertai Test | Lolos. Setiap jenis penolakan dan penghapusan file punya test |
| III. Data Produksi Dilindungi | Lolos setelah amandemen 2.0.0 yang mengatur pengecualian ini. Migrasi aditif |
| IV. Push ke Master Adalah Deploy | Lolos. Push menunggu persetujuan |
| V. Akses dan Keamanan | Lolos. Hanya Admin, antiforgery, dan validasi ulang di server |

## Design Decisions

| Keputusan | Alasan |
|---|---|
| Hanya dari Data Terhapus | Dua tahap memberi jeda, dan salah klik di daftar utama tidak bisa menghapus selamanya |
| Halaman konfirmasi sendiri, bukan popup | Dampak bisa dibaca dulu, dan alasan serta teks konfirmasi divalidasi server |
| Satu `SaveChanges` untuk log dan penghapusan | Tidak ada penghapusan tanpa log, dan tidak ada log tanpa penghapusan |
| `PermanentDeletionLogs` tanpa foreign key | Log harus lebih panjang umurnya daripada data dan akun yang disebut |
| Trigger menolak `UPDATE` dan `DELETE` | Log append-only di level database, bukan hanya di aplikasi |
| Gagal hapus file ditulis sebagai baris baru | Baris lama tidak boleh diubah |
| Pemakai dibaca dari metadata EF | Relasi baru otomatis ikut diperiksa |
| Foreign key database sebagai pengaman terakhir | Kalau pemeriksaan terlewat, `SaveChanges` gagal dan tidak ada yang terhapus |
| Log tanpa salinan data | "Selamanya" tetap benar, dan data pribadi tidak menumpuk di log |

## Risks

- File di MinIO belum punya backup, jadi file yang terhapus tidak bisa dikembalikan.
- Backup database masih memuat data sampai retensi 14 hari habis.
- Notifikasi lama yang menunjuk job terhapus akan membuka halaman tidak ditemukan.
