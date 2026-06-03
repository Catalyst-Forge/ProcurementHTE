# 🏢 ProcurementHTE
## Sistem Manajemen Pengadaan (Procurement Management System)
### PT Patra Drilling Contractor

---

## 📖 Deskripsi Proyek

**ProcurementHTE** adalah sistem manajemen pengadaan terintegrasi yang dikembangkan untuk PT Patra Drilling Contractor. Sistem ini dirancang untuk mengelola seluruh siklus procurement mulai dari pembuatan Purchase Requisition, pengelolaan vendor, proses approval multi-level, hingga penerbitan Purchase Order.

Sistem ini mengotomatisasi workflow approval berdasarkan nilai kontrak (Contract Total) dan menyediakan dashboard real-time untuk monitoring status pengadaan di berbagai level organisasi.

---

## 🛠️ Tech Stack

| Layer | Teknologi |
|-------|-----------|
| **Backend Framework** | ASP.NET Core 8.0 MVC |
| **ORM** | Entity Framework Core |
| **Database** | SQL Server |
| **Authentication** | ASP.NET Identity + JWT Token |
| **Real-time Communication** | SignalR |
| **Object Storage** | MinIO |
| **PDF Generation** | Custom HTML-to-PDF Engine |
| **QR Code** | QRCoder Library |
| **Architecture Pattern** | Clean Architecture |

---

## 🏗️ Arsitektur Proyek

```
ProcurementHTE/
├── ProcurementHTE.Core/          # Domain Layer - Entities, Interfaces, Enums
├── ProcurementHTE.Infrastructure/ # Infrastructure Layer - Data Access, External Services
├── ProcurementHTE.Web/           # Presentation Layer - Controllers, Views, APIs
└── ProcurementHTE.Core.Tests/    # Unit Tests
```

---

## ✨ Fitur Utama

### 🔐 1. Autentikasi & Keamanan

- **JWT Authentication** dengan Refresh Token mechanism
- **Two-Factor Authentication (2FA)**
  - Email OTP
  - SMS OTP
  - Authenticator App (TOTP)
- **Session Management** - Multi-device session tracking dengan kemampuan revoke
- **Security Audit Log** - Pencatatan lengkap aktivitas keamanan:
  - Login Success/Failed
  - Password Changes
  - 2FA Enable/Disable
  - Profile Updates
  - Session Revocations
- **Role-Based Access Control (RBAC)** dengan permission granular
- **Avatar Upload** dengan secure object storage

---

### 📦 2. Manajemen Procurement

- **CRUD Operations** dengan soft delete
- **Data Lengkap Procurement:**
  - Nomor SPK, WO Number, Project Code
  - Contract Type (Lumpsum, Unit Rate, Cost Plus, dll.)
  - Job Type dengan kategorisasi
  - Tanggal kontrak (Start/End Date)
  - Project Region (Sumbagut, Sumbagsel, Sumatera, dll.)
  - PIC Assignment per level approval
- **Publish/Unpublish** - Kontrol visibilitas data
- **Link to Purchase Requisition** - Integrasi dengan PR

---

### ✅ 3. Multi-Level Approval System

Sistem approval dinamis berdasarkan **Contract Total (CT)**:

| Nilai Kontrak | Level Approval |
|---------------|----------------|
| ≤ Rp 500 Juta | Analyst → Asst. Manager → Manager |
| ≤ Rp 5 Miliar | + Vice President |
| ≤ Rp 10 Miliar | + Operation Director |
| > Rp 10 Miliar | + President Director |

**Fitur Approval:**
- ✅ QR Code Token untuk verifikasi digital
- ✅ Rejection dengan kategorisasi symptoms
- ✅ Return for Revision - Sequential revision flow
- ✅ Document-based approval rules
- ✅ Real-time notification ke approver berikutnya

---

### 📄 4. Document Generation & Management

**Auto-Generated Documents:**
| No | Dokumen | Deskripsi |
|----|---------|-----------|
| 1 | Memorandum | Nota dinas internal |
| 2 | SPMP | Surat Perintah Mulai Pekerjaan |
| 3 | RKS | Rencana Kerja & Syarat-syarat |
| 4 | Risk Assessment | Penilaian risiko proyek |
| 5 | Owner Estimate | Estimasi biaya owner |
| 6 | BOQ | Bill of Quantity |
| 7 | Profit & Loss | Perhitungan laba/rugi |
| 8 | Justifikasi | Dokumen justifikasi pengadaan |

**Fitur Document Management:**
- Template-based generation (HTML → PDF)
- Upload dokumen dengan MinIO storage
- Presigned URL dengan expiry untuk secure access
- Version tracking

---

### 💰 5. Profit & Loss (P&L) Management

- **Multi-Vendor Comparison** - Perbandingan penawaran dari multiple vendor
- **Round-based Offers** - Tracking negosiasi per round
- **Automatic Calculation:**
  - Selected Vendor Final Offer
  - Profit Amount & Percentage
  - Accrual Amount
  - Realization Amount
- **Item-level Detail** - P&L breakdown per item
- **PDF Export** - Laporan P&L dalam format PDF

---

### 🏢 6. Vendor Management

- **Vendor Database** dengan data lengkap:
  - Kode Vendor, Nama, NPWP
  - Alamat lengkap (Kota, Provinsi, Kode Pos)
  - Email & Contact Information
- **Vendor Offers** - Tracking penawaran per procurement
- **Vendor Performance** - Analytics performa vendor
- **Vendor Round Letters** - Surat undangan negosiasi

---

### 📋 7. Purchase Requisition (PR)

- **Link Multiple Procurements** - Satu PR dapat berisi multiple items
- **Derived Status** - Status otomatis berdasarkan status procurement terkait:
  - Jika ada procurement Rejected → PR Rejected
  - Jika semua procurement DonePO → PR DonePO
  - Otherwise → Status minimum dari procurements
- **Document Upload** dengan object storage
- **ISPA Integration** - Integrasi dengan sistem ISPA
- **Complete Status History** - Audit trail lengkap

---

### 📊 8. Dashboard & Analytics

**Role-Specific Dashboards:**

| Dashboard | Pengguna |
|-----------|----------|
| Admin Dashboard | System Administrator |
| Analyst HTE Dashboard | Analyst Team |
| Assistant Manager Dashboard | Asst. Manager Level |
| Manager Transport Dashboard | Manager Level |
| Vice President Dashboard | VP Level |
| Operation Dashboard | Operation Director |
| APPO Dashboard | APPO Team |
| SCM Dashboard | Supply Chain Management |
| AR Dashboard | Accounts Receivable |
| AP Invoice Dashboard | Accounts Payable |
| HSE Dashboard | Health, Safety & Environment |

**Dashboard Metrics:**
- 📈 Active Procurements Count
- ⏳ Pending Approvals Count
- 💵 Total Revenue / Cost / Profit
- 📊 Monthly Procurement Trend
- 📉 Job Type Distribution
- 🏆 Top Vendors Performance
- 🗺️ Region Distribution
- 💹 Accrual Statistics

---

### 🔔 9. Real-time Notifications

- **SignalR WebSocket** untuk notifikasi instant
- **User Activity Tracking** - Online/Offline status monitoring
- **Role-based Notifications** - Notifikasi sesuai role
- **Notification Types:**
  - 📢 Procurement Published
  - ✅ Document Approved (per approval level)
  - ❌ PR Rejected
  - 🎉 PR Completed

---

### 🔄 10. Workflow & Pickup System

**Procurement Lifecycle:**
```
OnCreateDP3 → WaitingApproval (Multi-level) → OnSubmitISPA → OnSubmitHardcopy → OnSubmitPO → DonePO
```

**Pickup Features:**
- **APPO Pickup** - Assign procurement ke APPO user
- **AR Pickup** - Entry data accrual
- **AP Invoice Pickup** - Processing invoice
- **SCM Integration** - Submit PO number

---

### 📱 11. RESTful API

- Token-based authentication (JWT Bearer)
- Approval API untuk integrasi mobile/external apps
- Notifications API
- Pagination & Search support
- Consistent response format

---

### 🛡️ 12. Security Features

**Permission System:**
```
Procurement: Read, Create, Edit, Delete
Vendor: Read, Create, Edit, Delete
Document: Read, Upload, Approve
```

**Security Measures:**
- 🔒 Soft Delete - Data preservation untuk audit
- 📝 Audit Trail - Semua aktivitas tercatat
- 🔐 Secure File Access - Presigned URLs dengan expiry
- 🛡️ Input Validation - Server-side validation
- 🔑 Password Hashing - ASP.NET Identity security

---

## 📸 Screenshots

*[Tambahkan screenshots aplikasi di sini]*

---

## 🎯 Key Achievements

- ✅ Mengotomatisasi workflow approval yang sebelumnya manual
- ✅ Mengurangi waktu proses pengadaan hingga 60%
- ✅ Real-time visibility untuk management decision making
- ✅ Paperless document generation dan approval
- ✅ Secure dan auditable system

---

## 👨‍💻 Role & Responsibilities

- Merancang arsitektur sistem menggunakan Clean Architecture
- Mengembangkan backend dengan ASP.NET Core
- Implementasi JWT Authentication & Authorization
- Mengembangkan real-time features dengan SignalR
- Integrasi dengan MinIO untuk object storage
- Menulis unit tests untuk core business logic
- Database design dan migrations dengan EF Core

---

## 📅 Timeline

**Periode Pengembangan:** 2025 - 2026

---

## 📞 Kontak

*[Tambahkan informasi kontak Anda di sini]*

---

> **Note:** Proyek ini dikembangkan untuk PT Patra Drilling Contractor. Semua data yang ditampilkan adalah dummy data untuk keperluan demonstrasi.
