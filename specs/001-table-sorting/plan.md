# Implementation Plan: Sort Tabel Lewat Header Kolom

**Branch**: `001-table-sorting` | **Date**: 2026-11-07 | **Spec**: [spec.md](spec.md)

## Summary

Daftar berpaginasi diurutkan di server lewat query string `sort` dan `dir`. Tabel tanpa paginasi diurutkan
di browser oleh satu skrip global.

## Technical Context

- **Stack**: .NET 9, ASP.NET Core MVC, EF Core, SQL Server, htmx, Bootstrap Icons.
- **Testing**: xUnit dengan InMemory, ditambah SQL hasil `ToQueryString` yang dijalankan di SQL Server.
- **Constraints**: tidak ada migrasi, tidak ada perubahan skema.

## Constitution Check

| Prinsip | Status |
|---|---|
| I. Batas Lapisan | Lolos. `ListSort` dan `IListSortProvider` ada di Core, pembaca HTTP ada di Web |
| II. Perilaku Baru Disertai Test | Lolos. Query baru dibuktikan di SQL Server |
| III. Data Produksi Dilindungi | Lolos. Hanya baca, tanpa migrasi |
| IV. Push ke Master Adalah Deploy | Lolos. Push menunggu persetujuan |
| V. Akses dan Keamanan | Lolos. Kolom dibatasi daftar yang diizinkan, jadi URL tidak bisa memilih kolom bebas |

## Design Decisions

| Keputusan | Alasan | Alternatif yang ditolak |
|---|---|---|
| Repository membaca urutan lewat `IListSortProvider` | Satu titik perubahan per query, tanpa mengubah 13 tanda tangan controller dan service | Meneruskan parameter lewat 3 lapisan |
| Peta kolom per entitas, dengan `SortBy` generik | Daftar yang diizinkan eksplisit dan bisa diuji | Nama properti bebas dari URL |
| Tie-breaker pada id | Paginasi tetap stabil saat nilai sort sama | Tanpa tie-breaker |
| Tag helper `th[sort-key]` menghasilkan tautan | htmx boost langsung bekerja, tanpa JavaScript | Skrip klik pada header |
| LDP diurutkan di memori setelah mapping | Sebagian besar kolom LDP dihitung dari relasi, jadi tidak bisa diurutkan di SQL | 46 ekspresi SQL |
| Kolom Roles di User Management tidak disortir | Nilainya banyak per user dan dimuat sesudah query | Subquery khusus |

## Project Structure

```text
ProcurementHTE.Core/Common/ListSort.cs                     Model, provider, dan SortBy
ProcurementHTE.Infrastructure/Repositories/*SortColumns.cs Peta kolom per entitas
ProcurementHTE.Web/TagHelpers/SortableHeaderTagHelper.cs   Header yang bisa diklik
ProcurementHTE.Web/Extensions/ListQueryExtensions.cs       Membaca dan menyusun query string
ProcurementHTE.Web/wwwroot/js/table-sort.js                Sort di browser
ProcurementHTE.Web/wwwroot/css/components/table-sort.css   Gaya header
```

## Risks

- LDP memuat semua baris hasil filter saat sort aktif. Ini aman untuk volume sekarang, tapi perlu dikaji
  kalau data mencapai puluhan ribu baris.
