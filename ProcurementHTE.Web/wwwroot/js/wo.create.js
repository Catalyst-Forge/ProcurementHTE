(() => {
  const root = document.getElementById("wo-new");
  const createFormUrl = root?.dataset.createFormUrl;
  const grid = document.getElementById("wo-type-grid");
  const host = document.getElementById("wo-form-host");
  if (!root || !createFormUrl || !grid || !host) return;

  const fetchAny = async (url, options = {}) => {
    try {
      const rsp = await fetch(url, {
        credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest", ...(options.headers || {}) },
        ...options,
      });
      const ct = rsp.headers.get("content-type") || "";
      if (ct.includes("application/json")) {
        const json = await rsp.json();
        return { ok: rsp.ok, status: rsp.status, isJson: true, json, html: "" };
      } else {
        const html = await rsp.text();
        return { ok: rsp.ok, status: rsp.status, isJson: false, json: null, html };
      }
    } catch (error) {
      return {
        ok: false,
        status: 0,
        isJson: false,
        json: null,
        html: `<div class="alert alert-danger">Terjadi kesalahan: ${error.message}</div>`,
      };
    }
  };

  // pilih tipe → load partial
  grid.addEventListener("click", async (e) => {
    const btn = e.target.closest(".choose-type");
    if (!btn) return;
    const type = btn.dataset.type;
    btn.disabled = true;
    const original = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Loading...';

    try {
      const { ok, html } = await fetchAny(`${createFormUrl}?type=${encodeURIComponent(type)}`);
      host.innerHTML = html;
      if (ok) {
        grid.classList.add("d-none");
        host.scrollIntoView({ behavior: "smooth", block: "start" });
      }
    } finally {
      btn.disabled = false;
      btn.innerHTML = original;
    }
  });

  // tombol kembali → tampilkan grid lagi
  host.addEventListener("click", (e) => {
    if (e.target.id === "btnBackToType" || e.target.closest("#btnBackToType")) {
      host.innerHTML = "";
      grid.classList.remove("d-none");
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  });

  // submit AJAX
  host.addEventListener("submit", async (e) => {
    const form = e.target.closest("#wo-form");
    if (!form) return;
    e.preventDefault();

    const submitBtn = form.querySelector('button[type="submit"]');
    const originalText = submitBtn ? submitBtn.innerHTML : null;
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Menyimpan...';
    }

    try {
      const res = await fetchAny(form.action, { method: "POST", body: new FormData(form) });

      // Sukses → server kirim JSON { redirectUrl }
      if (res.ok && res.isJson && res.json?.redirectUrl) {
        window.location.href = res.json.redirectUrl;
        return;
      }

      // Gagal validasi → server kirim partial HTML form + error
      host.innerHTML = res.html;
      host.scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (err) {
      host.innerHTML = `<div class="alert alert-danger">Terjadi kesalahan: ${err.message}</div>`;
    } finally {
      if (submitBtn) {
        submitBtn.disabled = false;
        if (originalText) submitBtn.innerHTML = originalText;
      }
    }
  });
})();
