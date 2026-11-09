// Cells of the AP-PO, AP-Invoice, Data Accrual, and AR tables: click to fill, saved in place.
// A cell is a <button data-finance-field> inside a table with data-field-url.
(function () {
  "use strict";

  if (window.__financeTableBound) return;
  window.__financeTableBound = true;

  const STATUS_CLASSES = {
    done: "badge bg-success-subtle text-success-emphasis border border-success-subtle",
    progress: "badge bg-warning-subtle text-warning-emphasis border border-warning-subtle",
  };

  function token() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
  }

  async function save(url, field, value) {
    const body = new FormData();
    body.append("field", field);
    body.append("value", value);
    body.append("__RequestVerificationToken", token());

    const response = await fetch(url, {
      method: "POST",
      body,
      headers: { Accept: "application/json" },
      credentials: "same-origin",
    });
    if (!response.ok && response.status !== 400) {
      return { ok: false, error: "Gagal menyimpan. Muat ulang halaman lalu coba lagi." };
    }
    return response.json();
  }

  function showStatus(row, done) {
    const badge = row?.querySelector("[data-finance-status]");
    if (!badge) return;
    badge.className = done ? STATUS_CLASSES.done : STATUS_CLASSES.progress;
    badge.textContent = done ? "Done" : "On Progress";
  }

  document.addEventListener("click", async (event) => {
    const cell = event.target.closest("button[data-finance-field]");
    if (!cell || !window.Swal) return;
    event.preventDefault();

    const kind = cell.dataset.financeInput;
    const isDate = kind === "date";
    const label = cell.dataset.financeLabel;

    const result = await window.Swal.fire({
      title: label,
      input: isDate ? "date" : "text",
      inputValue: cell.dataset.financeValue ?? "",
      inputAttributes: isDate
        ? { "aria-label": label }
        : kind === "money"
          ? { inputmode: "numeric", "aria-label": label, placeholder: "Contoh: 1.500.000" }
          : { maxlength: cell.dataset.financeMax ?? "150", "aria-label": label },
      inputLabel: kind === "money" ? "Rupiah, tanpa Rp. Kosongkan untuk menghapus isian." : "Kosongkan untuk menghapus isian.",
      showCancelButton: true,
      confirmButtonText: "Simpan",
      cancelButtonText: "Batal",
      confirmButtonColor: "var(--dx-blue-600)",
      cancelButtonColor: "var(--dx-ink-muted)",
      reverseButtons: true,
      showLoaderOnConfirm: true,
      allowOutsideClick: () => !window.Swal.isLoading(),
      preConfirm: async (value) => {
        try {
          const answer = await save(cell.dataset.financeUrl, cell.dataset.financeField, value ?? "");
          if (!answer.ok) {
            window.Swal.showValidationMessage(answer.error ?? "Gagal menyimpan.");
            return false;
          }
          return { answer, value: (value ?? "").trim() };
        } catch {
          window.Swal.showValidationMessage("Gagal menyimpan. Periksa koneksi lalu coba lagi.");
          return false;
        }
      },
    });

    if (!result.isConfirmed || !result.value) return;

    const { answer, value } = result.value;
    const text = cell.querySelector(".finance-cell-value");
    if (text) {
      text.textContent = answer.display;
      text.classList.toggle("finance-cell-empty", value === "");
    }
    cell.classList.toggle("is-empty", value === "");
    cell.dataset.financeValue = value;
    showStatus(cell.closest("tr"), answer.done);
  });
})();
