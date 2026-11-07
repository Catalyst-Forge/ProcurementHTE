# Feature Specification: Hapus Permanen

**Feature Branch**: `004-permanent-delete`
**Created**: 2026-11-08
**Status**: Implemented
**Input**: Admin bisa menghapus data selamanya dari Data Terhapus, dan jejaknya tetap tersimpan di log.

## User Scenarios & Testing

### User Story 1 - Menghapus data terhapus selamanya (Priority: P1)

Admin membuka Data Terhapus, memilih satu data, membaca dampaknya, mengisi alasan, mengetik `HAPUS`, lalu
datanya hilang selamanya.

**Independent Test**: Hapus permanen satu procurement terhapus, lalu pastikan baris turunan dan filenya hilang.

**Acceptance Scenarios**:

1. **Given** data ada di Data Terhapus, **When** Admin mengonfirmasi dengan alasan, **Then** data, turunannya,
   dan filenya hilang, dan satu baris log tertulis.
2. **Given** data belum dihapus, **When** hapus permanen diminta, **Then** ditolak.
3. **Given** alasan kurang dari 10 karakter atau teks konfirmasi salah, **When** dikirim, **Then** ditolak.
4. **Given** data masih dipakai data lain, **When** halaman konfirmasi dibuka, **Then** tampil daftar pemakainya
   dan tombol hapus tidak ada.

### User Story 2 - Membaca log hapus permanen (Priority: P1)

Admin membuka Riwayat Hapus Permanen dan melihat apa yang dihapus, siapa, kapan, dan kenapa.

**Acceptance Scenarios**:

1. **Given** data sudah dihapus permanen, **When** Riwayat dibuka, **Then** barisnya tampil.
2. **Given** akun yang menghapus ikut dihapus permanen, **When** Riwayat dibuka, **Then** namanya tetap tampil.
3. **Given** baris log, **When** ada yang mengubah atau menghapusnya lewat SQL, **Then** database menolak.

### Edge Cases

- File yang masih dipakai data lain, termasuk data terhapus yang masih bisa dipulihkan, tidak ikut dihapus.
- Gagal menghapus file tidak mengembalikan data, tetapi tercatat sebagai baris log baru.
- Vendor yang dipakai P&L, penawaran, atau detail ditolak, walaupun relasinya cascade.
- Akun dengan riwayat di job, PR, atau aturan ditolak.
- Log tidak menyimpan salinan data, hanya identitas, ringkasan, dan jumlah.
