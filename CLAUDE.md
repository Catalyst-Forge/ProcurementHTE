# Docutrax (ProcurementHTE)

Aplikasi pengadaan berbasis ASP.NET Core MVC.

Aturan project ada di `.specify/memory/constitution.md`. Baca sebelum mengubah kode.

## Dokumen

- Produk: `PRODUCT.md`
- Desain: `DESIGN.md`
- Deploy dan server: `deployment/production/README.md`
- Spec, plan, dan tasks per fitur: `specs/<nomor>-<nama>/`

## Perintah

```bash
dotnet build ProcurementHTE.sln
dotnet test ProcurementHTE.Tests
```

## Spec Kit

Perubahan besar melewati `/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, lalu `/speckit-implement`.
Perbaikan kecil tidak perlu lewat alur ini.

Skill Spec Kit ada di `.claude/skills/`, yang di-gitignore. Di mesin baru, pasang dengan
`specify integration install claude`.
