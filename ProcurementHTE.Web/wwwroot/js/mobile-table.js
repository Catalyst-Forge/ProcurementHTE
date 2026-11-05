// Fills data-label on the cells of every table.mobile-table from its header, so a view
// only has to add the class to get the card layout on phones (css/components/mobile-table.css).
// Cells that already carry a data-label keep it. Runs on load, after HTMX swaps, and
// whenever rows are added later (realtime updates, client-side rendering).
(function () {
  "use strict";

  function headerLabels(table) {
    const headRow = table.tHead?.rows[table.tHead.rows.length - 1];
    if (!headRow) return null;

    const labels = [];
    for (const th of headRow.cells) {
      const text = th.textContent.replace(/\s+/g, " ").trim();
      for (let i = 0; i < (th.colSpan || 1); i++) labels.push(text);
    }
    return labels;
  }

  function labelTable(table) {
    const labels = headerLabels(table);
    if (!labels) return;

    for (const body of table.tBodies) {
      for (const row of body.rows) {
        let col = 0;
        for (const cell of row.cells) {
          const label = labels[col];
          if (!cell.hasAttribute("data-label") && label && cell.colSpan === 1) {
            cell.setAttribute("data-label", label);
          }
          col += cell.colSpan || 1;
        }
      }
    }
  }

  function labelAll(root) {
    const scope = root && root.querySelectorAll ? root : document;
    if (scope.matches?.("table.mobile-table")) labelTable(scope);
    scope.querySelectorAll("table.mobile-table").forEach(labelTable);
  }

  let pending = false;
  function scheduleLabelAll() {
    if (pending) return;
    pending = true;
    requestAnimationFrame(() => {
      pending = false;
      labelAll(document);
    });
  }

  function start() {
    labelAll(document);
    document.body.addEventListener("htmx:afterSwap", (e) => labelAll(e.detail?.target));

    new MutationObserver((mutations) => {
      for (const m of mutations) {
        for (const node of m.addedNodes) {
          if (node.nodeType === 1 && (node.closest?.("table.mobile-table") || node.querySelector?.("table.mobile-table"))) {
            scheduleLabelAll();
            return;
          }
        }
      }
    }).observe(document.body, { childList: true, subtree: true });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", start);
  } else {
    start();
  }
})();
