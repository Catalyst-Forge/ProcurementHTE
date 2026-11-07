<!--
Sync Impact Report
- Version change: none -> 1.0.0 (first ratification)
- Modified principles: none
- Added principles: I. Batas Lapisan, II. Perilaku Baru Disertai Test, III. Data Produksi Dilindungi,
  IV. Push ke Master Adalah Deploy, V. Akses dan Keamanan
- Added sections: Batasan Teknis, Alur Kerja, Governance
- Removed sections: none
- Templates: plan-template.md reads this file at runtime through its Constitution Check; no edits needed
- Follow-up TODOs: none
-->

# Docutrax (ProcurementHTE) Constitution

## Core Principles

### I. Batas Lapisan

Kode dibagi menjadi tiga project dengan arah ketergantungan satu arah:

- `ProcurementHTE.Core` memegang model, aturan bisnis, service, dan interface.
- `ProcurementHTE.Infrastructure` memegang EF Core, repository, dan integrasi.
- `ProcurementHTE.Web` memegang controller, view, dan aset.

Aturannya:

- Controller dan view WAJIB tidak mengakses `AppDbContext` secara langsung.
- Kode baru di Core WAJIB tidak menambah ketergantungan ke ASP.NET Core MVC atau EF Core.

Alasan: aturan bisnis tetap bisa diuji tanpa database dan tanpa web.

### II. Perilaku Baru Disertai Test

- Perbaikan bug dan aturan bisnis baru WAJIB punya test yang gagal tanpa perubahan itu.
- `dotnet test ProcurementHTE.Tests` WAJIB lolos sebelum push.
- Bentuk query LINQ baru WAJIB dibuktikan terhadap SQL Server, karena provider InMemory tidak
  membuktikan query itu bisa diterjemahkan.

Alasan: test berjalan di InMemory, sedangkan produksi berjalan di SQL Server.

### III. Data Produksi Dilindungi

- Data bisnis WAJIB dihapus lewat soft delete (`IsDeleted`), bukan hard delete.
- Perubahan pada database produksi WAJIB mendapat persetujuan eksplisit setiap kali.
- Migrasi WAJIB aditif, didahului backup, dan skrip idempotent-nya diperiksa sebelum dijalankan.
- Migrasi WAJIB diterapkan ke produksi sebelum push, dengan `sqlcmd -I`.

Alasan: aplikasi lokal memakai database produksi dan tidak menjalankan migrasi sendiri, jadi skema
yang tertinggal langsung membuatnya error.

### IV. Push ke Master Adalah Deploy

- Server menarik `master` secara berkala dan langsung men-deploy-nya, walaupun CI gagal.
- Push ke `master` WAJIB atas persetujuan eksplisit, termasuk perubahan yang tampak kecil, karena
  setiap commit baru membuat aplikasi restart.
- Setelah push, hasil CI dan kesehatan aplikasi WAJIB diperiksa.

Alasan: CI hanya memberi tahu, tidak memblokir deploy.

### V. Akses dan Keamanan

- Halaman alur kerja terbuka untuk semua role yang login. Aksi yang mengubah data WAJIB dibatasi
  lewat `ActionRoles`, baik di controller maupun di tombolnya.
- Master Data, Rules & Mapping, dan User Management WAJIB hanya untuk Admin.
- Action MVC yang mengubah data WAJIB memakai `[ValidateAntiForgeryToken]`.
- Admin WAJIB tidak bisa menghapus atau menonaktifkan akunnya sendiri, atau melepas role Admin
  dari akunnya sendiri.
- Password, token, dan connection string WAJIB tidak masuk repo.

Alasan: menyembunyikan tombol tidak cukup, dan aplikasi tidak boleh kehilangan Admin terakhirnya.

## Batasan Teknis

- Stack: .NET 9, ASP.NET Core MVC dengan Razor, EF Core, SQL Server, htmx, Bootstrap, SweetAlert,
  MinIO, dan xUnit.
- Teks yang dilihat pengguna WAJIB berbahasa Indonesia.

## Alur Kerja

- Perubahan besar, yaitu yang lintas modul, mengubah aturan bisnis, atau mengubah skema, WAJIB
  melewati `/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, lalu `/speckit-implement`.
- Perbaikan kecil dan bug boleh langsung dikerjakan tanpa spec, dengan tetap mematuhi prinsip di atas.
- Selama repo public, spec yang berisi aturan bisnis klien WAJIB tidak di-commit.

## Governance

Constitution ini mengalahkan kebiasaan kerja lain di project ini. Setiap plan WAJIB lolos Constitution
Check, dan pengecualian WAJIB dicatat beserta alasannya di bagian Complexity Tracking pada plan.

Perubahan dilakukan lewat `/speckit-constitution` dengan penomoran semantik: MAJOR untuk menghapus atau
mengubah makna prinsip, MINOR untuk menambah prinsip atau bagian, dan PATCH untuk perbaikan kata.

**Version**: 1.0.0 | **Ratified**: 2026-11-07 | **Last Amended**: 2026-11-07
