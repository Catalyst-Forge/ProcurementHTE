// Marks the empty cells of every .table with .cell-empty so site.css centres them under the
// column title, whatever the column's own alignment. A cell is empty when all it shows is
// "-", or only the fill-in icons of the finance tables (.finance-cell-edit.is-empty).
// Runs on load, after HTMX swaps, when rows are added later, and after a finance cell is
// saved (finance-table.js calls window.markEmptyCells with the cell).
(function () {
  "use strict";

  const DASHES = new Set(["-", "–", "—"]);

  function visibleText(cell) {
    let text = "";
    const walker = document.createTreeWalker(cell, NodeFilter.SHOW_TEXT, {
      acceptNode: (node) =>
        node.parentElement?.closest(".finance-cell-edit.is-empty, .visually-hidden")
          ? NodeFilter.FILTER_REJECT
          : NodeFilter.FILTER_ACCEPT,
    });
    while (walker.nextNode()) text += walker.currentNode.nodeValue;
    return text.replace(/\s+/g, "");
  }

  function isEmpty(cell) {
    if (cell.colSpan > 1) return false;
    const text = visibleText(cell);
    if (DASHES.has(text)) return true;
    return text === "" && cell.querySelector(".finance-cell-edit.is-empty") !== null;
  }

  function markCell(cell) {
    cell.classList.toggle("cell-empty", isEmpty(cell));
  }

  function markEmptyCells(root) {
    const scope = root && root.querySelectorAll ? root : document;
    if (scope.matches?.("td")) {
      if (scope.closest("table.table")) markCell(scope);
      return;
    }
    scope.querySelectorAll("table.table > tbody > tr > td").forEach(markCell);
  }

  window.markEmptyCells = markEmptyCells;

  let pending = false;
  function scheduleMark() {
    if (pending) return;
    pending = true;
    requestAnimationFrame(() => {
      pending = false;
      markEmptyCells(document);
    });
  }

  function start() {
    markEmptyCells(document);
    document.body.addEventListener("htmx:afterSwap", (e) => markEmptyCells(e.detail?.target));

    new MutationObserver((mutations) => {
      for (const m of mutations) {
        for (const node of m.addedNodes) {
          if (node.nodeType === 1 && (node.closest?.("table.table") || node.querySelector?.("table.table"))) {
            scheduleMark();
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
