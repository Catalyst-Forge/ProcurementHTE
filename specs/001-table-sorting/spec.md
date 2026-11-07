# Feature Specification: Sort Tabel Lewat Header Kolom

**Feature Branch**: `001-table-sorting`
**Created**: 2026-11-07
**Status**: Implemented
**Input**: Permintaan klien: "seluruh tabel dibuatkan ascending dan descending, bisa diklik di header kolom."

## User Scenarios & Testing

### User Story 1 - Mengurutkan daftar utama (Priority: P1)

Pengguna membuka daftar berpaginasi, misalnya Vendors, lalu mengklik header "Vendor Name". Seluruh data
terurut A ke Z, bukan hanya halaman yang sedang tampil. Klik lagi membalik urutannya.

**Why this priority**: Daftar utama adalah tempat pengguna mencari data setiap hari, dan urutan yang hanya
berlaku di satu halaman menyesatkan.

**Independent Test**: Buka Vendors dengan lebih dari satu halaman data, klik header, lalu pastikan vendor
pertama secara alfabet ada di halaman 1 dan urutan berlanjut di halaman 2.

**Acceptance Scenarios**:

1. **Given** daftar dengan banyak halaman, **When** header diklik, **Then** seluruh data terurut naik dan
   tampilan kembali ke halaman 1.
2. **Given** kolom sedang terurut naik, **When** header yang sama diklik, **Then** urutan menjadi turun.
3. **Given** data terurut, **When** pengguna pindah halaman atau mengubah jumlah baris, **Then** urutan dan
   pencarian tetap berlaku.

### User Story 2 - Mengurutkan tabel kecil (Priority: P2)

Pengguna mengklik header pada tabel tanpa paginasi, misalnya tabel dashboard atau detail procurement, dan
barisnya langsung terurut tanpa memuat ulang halaman.

**Independent Test**: Klik header kolom tanggal dan nilai uang, lalu pastikan urutannya mengikuti tanggal
dan angka, bukan urutan teks.

**Acceptance Scenarios**:

1. **Given** tabel tanpa paginasi, **When** header diklik, **Then** baris terurut dan klik berikutnya
   membalik urutan.
2. **Given** kolom berisi tanggal atau nominal, **When** diurutkan, **Then** urutannya berdasarkan nilai.

### Edge Cases

- Nilai kosong atau `-` berada di awal saat naik dan di akhir saat turun, sama seperti NULL di SQL Server.
- Parameter sort yang tidak dikenal diabaikan, dan daftar memakai urutan bawaannya.
- Nilai sort yang sama tetap punya urutan pasti, supaya baris tidak muncul dua kali di halaman berbeda.
- Baris kosong ("tidak ada data") tidak ikut diurutkan.
- Tabel berisi isian form, tabel `rowspan`, dan tabel label-nilai tidak diberi sort.

## Requirements

### Functional Requirements

- **FR-001**: Setiap kolom data pada daftar berpaginasi MUST bisa diurutkan lewat klik header.
- **FR-002**: Daftar berpaginasi MUST mengurutkan seluruh data hasil filter, bukan hanya halaman aktif.
- **FR-003**: Klik header pertama MUST mengurutkan naik, dan klik pada kolom yang sama MUST membaliknya.
- **FR-004**: Header MUST menunjukkan kolom aktif dan arahnya, termasuk untuk pembaca layar lewat `aria-sort`.
- **FR-005**: Urutan dan pencarian MUST tetap berlaku saat berpindah halaman dan mengubah jumlah baris.
- **FR-006**: Tabel tanpa paginasi MUST bisa diurutkan di browser berdasarkan teks, angka, dan tanggal.
- **FR-007**: Kolom non-data seperti No, Aksi, dan centang MUST tidak bisa diurutkan.
- **FR-008**: Sort MUST bisa dipakai dengan keyboard.
- **FR-009**: Parameter sort dari URL MUST dibatasi ke daftar kolom yang diizinkan.

### Key Entities

- **Urutan daftar**: kolom yang dipilih dan arahnya, dibawa lewat URL.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Semua kolom data pada 13 daftar berpaginasi bisa diurutkan dengan satu klik.
- **SC-002**: Query setiap kolom yang diurutkan di server berjalan tanpa error di SQL Server.
- **SC-003**: Halaman yang tidak disortir tetap memakai urutan bawaannya.

## Assumptions

- Di bawah 992px tabel tampil sebagai kartu tanpa header, jadi sort tidak tersedia di HP.
- Pencarian baru dan pindah tab kembali ke urutan bawaan.
- Daftar berpaginasi yang dicakup: Procurements, PR, LDP, Vendors, Document Types, Procurement Types,
  User Management, AP-PO (2 tab), AP Invoice (2 tab), AR (2 tab), Data Accrual, dan Pending Approvals.
- Tampilan `Tenders` dan `ProcurementOrder` tidak punya controller, jadi tidak dicakup.
