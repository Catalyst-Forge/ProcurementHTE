# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Karyawan PT Patra Drilling Contractor (Pertamina PDC) yang menjalankan proses pengadaan: pembuat Purchase Requisition, approver bertingkat, pengelola vendor, dan admin pengguna. Mereka bekerja di kantor dengan desktop/laptop sebagai perangkat utama, dan kadang membuka aplikasi dari ponsel untuk approval.

## Product Purpose

Docutrax adalah aplikasi internal untuk mengelola dan melacak dokumen serta proses pengadaan, mulai dari PR, approval multi-level berdasarkan nilai kontrak, vendor, sampai PO, lengkap dengan dashboard status real-time. "Procurement HTE" adalah nama lama dan sekarang dipakai sebagai nama salah satu modul/fitur di dalam Docutrax.

## Operating Context

- Aplikasi intranet perusahaan. Klien juga punya aplikasi internal lain (INPRO, "Integrated Procurement") yang tampilannya dijadikan referensi gaya oleh klien.
- Login memakai NIP atau email ditambah password dan captcha matematika. Setelah itu bisa diminta 2FA (email, SMS, atau aplikasi authenticator).
- NIP diisi manual oleh admin lewat halaman kelola user. Selama masa transisi, user yang belum punya NIP tetap bisa login memakai email.

## Capabilities and Constraints

- ASP.NET Core 8 MVC + Razor views, Bootstrap 5, Bootstrap Icons, jQuery, SweetAlert2, SignalR. Aset disajikan dari `wwwroot/lib` tanpa CDN.
- Bahasa antarmuka: Indonesia.
- Produksi berjalan di VPS sendiri (systemd + nginx) dengan database SQL Server.

## Brand Commitments

- Nama produk: **Docutrax**. Logo: simbol infinity bergradasi biru (#2563EB) ke teal (#14B8A6) di atas tulisan "DOCUTRAX" berwarna navy, huruf kapital dengan spasi lebar. Logo dikirim oleh user.
- Klien meminta halaman login mengikuti komposisi referensi INPRO: panel visual besar di kiri, kartu login putih di kanan berisi logo, field identitas, password dengan tombol tampilkan/sembunyikan, captcha, dan tombol login biru.
- Belum diputuskan apakah logo Pertamina PDC ikut tampil di login, jadi jangan ditambahkan sebelum dikonfirmasi.

## Evidence on Hand

- Logo Docutrax (raster berlatar putih): diterima lewat percakapan. Versi vektor asli belum ada.
- Logo Pertamina PDC: `ProcurementHTE.Web/wwwroot/images/logo.png`.
- Belum ada tagline, ilustrasi, atau maskot resmi Docutrax, jadi jangan mengarang klaim atau angka statistik.

## Product Principles

- Masuk ke aplikasi harus cepat dan jelas, karena ini pintu harian karyawan, bukan halaman pemasaran.
- Identitas Docutrax tampil percaya diri, tapi tidak menghalangi tugas.
- Keamanan terlihat dan bisa dipahami (captcha, 2FA, pesan error yang jelas), tanpa terasa menghukum pengguna.
