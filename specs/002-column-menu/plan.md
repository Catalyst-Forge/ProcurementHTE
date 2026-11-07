# Implementation Plan: Menu Kolom

**Branch**: `002-column-menu` | **Date**: 2026-11-07 | **Spec**: [spec.md](spec.md)

## Summary

Tag helper dan `table-sort.js` menghasilkan tombol header yang sama. Satu skrip `column-menu.js` membuka card
untuk tombol itu. Filter LDP dibawa lewat query string `f.<kolom>` dan dicocokkan setelah mapping.

## Constitution Check

| Prinsip | Status |
|---|---|
| I. Batas Lapisan | Lolos. Kontrak filter ada di Core, pembaca HTTP di Web |
| II. Perilaku Baru Disertai Test | Lolos. Filter berjalan di memori, jadi tidak ada query SQL baru |
| III. Data Produksi Dilindungi | Lolos. Hanya baca, tanpa migrasi |
| IV. Push ke Master Adalah Deploy | Lolos. Push menunggu persetujuan |
| V. Akses dan Keamanan | Lolos. Kunci filter dibatasi properti DTO, nilai hanya dipakai untuk pencocokan teks |

## Design Decisions

| Keputusan | Alasan |
|---|---|
| Satu elemen card di `body`, posisi `fixed` | Tidak terpotong `.table-responsive`, dan tidak ada 46 card di DOM |
| Navigasi lewat tautan tersembunyi di `#app-content` | Tetap memakai htmx boost yang sudah ada |
| Chip dibuat dari tombol header, termasuk kolom tersembunyi | Label dan nilainya sudah ada di sana |
| Filter LDP mencocokkan teks yang tampil | Hasil sama dengan yang dilihat pengguna |

## Risks

- Filter LDP memuat semua baris, sama seperti sort LDP.
