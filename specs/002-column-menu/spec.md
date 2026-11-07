# Feature Specification: Menu Kolom

**Feature Branch**: `002-column-menu`
**Created**: 2026-11-07
**Status**: Implemented
**Input**: Klien meminta pencarian per kolom seperti Excel. Header tetap bersih, dan klik header membuka card berisi urutan naik, urutan turun, dan pencarian nilai.

## User Scenarios & Testing

### User Story 1 - Mencari nilai di satu kolom LDP (Priority: P1)

Staf mengklik header Vendor di LDP, mengetik "krakatau", lalu menekan Enter. Tabel hanya menampilkan baris
yang cocok dari seluruh data.

**Independent Test**: Terapkan filter di satu kolom, pindah tab, lalu pastikan chip filter tetap tampil.

**Acceptance Scenarios**:

1. **Given** LDP terbuka, **When** nilai dicari di satu kolom, **Then** hanya baris yang cocok tampil dan
   jumlah total ikut berubah.
2. **Given** filter aktif di kolom yang tersembunyi di tab sekarang, **When** tab dibuka, **Then** chip
   filter tetap tampil.
3. **Given** filter aktif, **When** chip dihapus, **Then** filter itu hilang dan filter lain tetap berlaku.
4. **Given** filter tidak menemukan data, **When** tabel tampil, **Then** ada pesan dan tautan hapus filter.

### User Story 2 - Mengurutkan lewat menu (Priority: P1)

Pengguna mengklik header kolom di tabel mana pun, lalu memilih arah urutan dari card.

**Acceptance Scenarios**:

1. **Given** tabel yang bisa disortir, **When** header diklik, **Then** card terbuka tanpa mengubah data.
2. **Given** card terbuka, **When** arah dipilih, **Then** urutan berlaku dan card menutup.
3. **Given** card terbuka, **When** Esc ditekan atau klik di luar, **Then** card menutup.

### Edge Cases

- Card tidak boleh terpotong oleh area scroll tabel, dan tetap masuk layar di tepi kanan atau bawah.
- Kolom No, Aksi, dan Link Dokumen tidak punya pencarian; No dan Aksi juga tidak punya menu.
- Kunci filter yang tidak dikenal diabaikan.

## Requirements

### Functional Requirements

- **FR-001**: Header MUST tidak menampilkan ikon saat kolom tidak disortir atau difilter.
- **FR-002**: Klik header MUST membuka satu card yang sama untuk semua kolom.
- **FR-003**: Label urutan MUST mengikuti jenis kolom: teks, tanggal, atau angka.
- **FR-004**: Kolom aktif MUST punya penanda urutan dan filter.
- **FR-005**: Pencarian LDP MUST berlaku pada seluruh data dan mencocokkan teks yang tampil.
- **FR-006**: Filter aktif MUST tampil sebagai chip di semua tab dan bisa dihapus satu per satu atau sekaligus.
- **FR-007**: Filter dan urutan MUST ikut terbawa saat pindah halaman.
- **FR-008**: Card MUST bisa dipakai dengan keyboard, dan fokus kembali ke header saat ditutup.

## Success Criteria

- **SC-001**: Lebar header idle tidak bertambah, dan tab LDP tetap muat mulai lebar 1280 piksel.
- **SC-002**: Semua kolom yang bisa disortir memakai card ini.

## Assumptions

- Tahap ini hanya punya satu kotak pencarian. Daftar centang dan rentang tanggal belum dibuat.
- Pencarian hanya tersedia di LDP. Daftar lain menampilkan pilihan urutan saja.
- Di bawah 992px header tersembunyi, jadi card tidak tersedia, tetapi chip tetap tampil.
