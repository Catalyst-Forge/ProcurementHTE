---
version: 1
slug: "procurementhte-web-views-auth-login-cshtml"
primary_target: "ProcurementHTE.Web/Views/Auth/Login.cshtml"
related_targets: ["ProcurementHTE.Web/Views/Shared/_LoginLayout.cshtml","ProcurementHTE.Web/wwwroot/css/login.css"]
---

# Login — Docutrax

Scope: halaman masuk (`Views/Auth/Login.cshtml` + `_LoginLayout`). Mode: Operate, karena tugasnya masuk ke aplikasi dengan cepat dan benar. Pengunjung adalah karyawan PDC yang membukanya setiap hari, umumnya di desktop kantor dan kadang di ponsel.

Konstanta dari klien: komposisi split mengikuti referensi INPRO (panel visual di kiri, kartu login putih di kanan). Isi form: NIP atau Email, password dengan toggle, captcha matematika, ingat saya, lupa password, dan tombol masuk biru. Halaman lain di bawah layout autentikasi tidak ikut berubah.

## Direction contract

THESIS: Docutrax melacak dokumen pengadaan tanpa putus, dan halaman login membuktikannya. Panel kiri adalah simbol infinity Docutrax dalam ukuran besar sebagai jalur, tempat dokumen bergerak melewati tahap PR → Approval → Vendor → PO. Halaman ini menolak pola bawaan kategorinya: foto stok kantor atau ilustrasi kilang generik dengan tagline motivasi.

OWN-WORLD: Panel kiri berupa bidang navy pekat (#0A1A3F ke #0F2A66) dengan grid titik halus. Jalur infinity bergradasi brand (#2563EB → #14B8A6) dengan lebar stroke seperti pada logo. Token dokumen putih dengan sudut terlipat, dan pil tahap putih bertepi tipis. Sisi kanan memakai latar tint biru-abu yang sangat muda dan kartu putih bersudut 16px dengan bayangan lembut ber-offset. Satu aksen biru #2563EB untuk aksi, teal hanya untuk status sukses dan fokus.

STORY: Dalam satu detik pengunjung tahu ini Docutrax, tahu tempat memasukkan NIP, lalu masuk. Panel kiri memberi tahu, tanpa klaim pemasaran, bahwa ini sistem yang melacak dokumen pengadaan dari PR sampai PO.

FIRST VIEWPORT: Pada lebar desktop, panel kiri mengisi sekitar 58% layar setinggi viewport. Jalur infinity sekitar 70% lebar panel di tengah agak ke atas, dengan empat pil tahap di titik-titik loop. Tagline dua baris rata kiri di bawah, berwarna putih, sekitar 40px. Di kanan, kartu 440px di tengah vertikal berisi logo di atas, h1 "Masuk ke Docutrax", form, dan tombol "Masuk" lebar penuh sebagai aksi utama. Di layar sempit (<992px) panel menjadi pita setinggi 200px berisi jalur saja, dan kartu mengisi lebar.

FORM: Split dua panel sesuai pin klien (bukan dari roll; arah dipin oleh brief). Gerakan khasnya adalah dokumen yang berjalan di sepanjang jalur infinity (CSS offset-path), dengan entrance satu kali saat jalur tergambar. Dengan prefers-reduced-motion, tampilan menjadi statis. Seed: pinned-by-brief.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
