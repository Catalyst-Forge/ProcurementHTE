// Hapus terpilih on a list (LDP, Arsip LDP). Inside a [data-bulk-scope]: a [data-bulk-item]
// checkbox per row, a [data-bulk-all] checkbox in the header for every row on the page, and
// the [data-bulk-bar] form (Views/Shared/_BulkDeleteBar.cshtml) that shows while any row is
// ticked. The row checkboxes join the bar's form through their form attribute, so nothing
// here sends anything; it only keeps the header box, the count, and the bar in step.
(function () {
  "use strict";

  const items = (scope) => Array.from(scope.querySelectorAll("[data-bulk-item]"));

  function refresh(scope) {
    const boxes = items(scope);
    const ticked = boxes.filter((box) => box.checked).length;

    const all = scope.querySelector("[data-bulk-all]");
    if (all) {
      all.disabled = boxes.length === 0;
      all.checked = boxes.length > 0 && ticked === boxes.length;
      all.indeterminate = ticked > 0 && ticked < boxes.length;
    }

    for (const box of boxes) box.closest("tr")?.classList.toggle("is-selected", box.checked);

    const bar = scope.querySelector("[data-bulk-bar]");
    if (!bar) return;
    bar.hidden = ticked === 0;
    const count = ticked.toLocaleString("id-ID");
    bar.querySelectorAll("[data-bulk-count]").forEach((node) => (node.textContent = count));
    bar.dataset.confirmTitle = `Hapus ${count} baris terpilih?`;
  }

  function refreshAll(root) {
    const base = root && root.querySelectorAll ? root : document;
    if (base.matches?.("[data-bulk-scope]")) refresh(base);
    base.querySelectorAll("[data-bulk-scope]").forEach(refresh);
  }

  document.addEventListener("change", (event) => {
    const box = event.target;
    if (!(box instanceof HTMLInputElement)) return;
    const scope = box.closest("[data-bulk-scope]");
    if (!scope) return;

    if (box.matches("[data-bulk-all]")) {
      for (const item of items(scope)) item.checked = box.checked;
    }
    if (box.matches("[data-bulk-all], [data-bulk-item]")) refresh(scope);
  });

  document.addEventListener("click", (event) => {
    const clear = event.target instanceof Element ? event.target.closest("[data-bulk-clear]") : null;
    const scope = clear?.closest("[data-bulk-scope]");
    if (!scope) return;
    for (const item of items(scope)) item.checked = false;
    refresh(scope);
  });

  // The browser may bring ticks back on reload or Back, so the bar follows what is ticked.
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", () => refreshAll(document));
  } else {
    refreshAll(document);
  }
  window.addEventListener("pageshow", () => refreshAll(document));
  document.addEventListener("htmx:afterSettle", (event) => refreshAll(event.detail?.elt));
})();
