// Click-to-sort and column search for tables without paging. Paged lists sort and search on
// the server instead, through <th sort-key filter-key>. Skipped: tables with data-no-sort,
// form fields, rowspans, or spanning rows.
(function () {
  "use strict";

  const MONTHS = {
    jan: 0, feb: 1, mar: 2, apr: 3, may: 4, mei: 4, jun: 5, jul: 6,
    aug: 7, agu: 7, agt: 7, sep: 8, oct: 9, okt: 9, nov: 10, dec: 11, des: 11,
  };
  const NON_DATA_HEADERS = new Set(["", "#", "no", "no.", "aksi", "action", "actions"]);
  const collator = new Intl.Collator("id", { numeric: true, sensitivity: "base" });

  function parseDate(text) {
    const match = text.match(/^(\d{1,2})[\s/-]([a-z]{3,9}|\d{1,2})[\s/-](\d{4})(?:\s+(\d{1,2}):(\d{2}))?/i);
    if (!match) return null;

    const [, day, monthText, year, hour = 0, minute = 0] = match;
    const month = /\d/.test(monthText) ? Number(monthText) - 1 : MONTHS[monthText.slice(0, 3).toLowerCase()];
    return month === undefined ? null : new Date(year, month, day, hour, minute).getTime();
  }

  function parseNumber(text) {
    const plain = text.replace(/^rp\.?\s*/i, "").replace(/\s*%$/, "");
    if (!/^-?\d{1,3}(\.\d{3})*(,\d+)?$|^-?\d+(,\d+)?$/.test(plain)) return null;
    return Number(plain.replace(/\./g, "").replace(",", "."));
  }

  function sortValue(cell) {
    const text = (cell?.dataset.sortValue ?? cell?.textContent ?? "").replace(/\s+/g, " ").trim();
    if (text === "" || text === "-") return { empty: true };
    return { text, number: parseNumber(text), date: parseDate(text) };
  }

  // Empty values sort lowest, as NULL does in the server-sorted lists.
  function compare(a, b) {
    if (a.empty || b.empty) return (b.empty ? 1 : 0) - (a.empty ? 1 : 0);
    if (a.date !== null && b.date !== null) return a.date - b.date;
    if (a.number !== null && b.number !== null) return a.number - b.number;
    return collator.compare(a.text, b.text);
  }

  function sortBy(table, button, descending) {
    const header = button.closest("th");
    const index = [...header.parentElement.cells].indexOf(header);
    const body = table.tBodies[0];

    const rows = [...body.rows].map((row) => ({ row, value: sortValue(row.cells[index]) }));
    rows.sort((a, b) => (descending ? compare(b.value, a.value) : compare(a.value, b.value)));
    rows.forEach(({ row }) => body.appendChild(row));

    table.tHead.querySelectorAll("th[aria-sort]").forEach((th) => {
      th.removeAttribute("aria-sort");
      const other = th.querySelector(".column-header");
      delete other.dataset.sorted;
      other.querySelector(".column-sort-mark")?.remove();
    });

    header.setAttribute("aria-sort", descending ? "descending" : "ascending");
    button.dataset.sorted = descending ? "desc" : "asc";
    const mark = document.createElement("i");
    mark.className = `bi ${descending ? "bi-caret-down-fill" : "bi-caret-up-fill"} column-sort-mark`;
    mark.setAttribute("aria-hidden", "true");
    button.append(mark);
  }

  const normalize = (text) => text.replace(/\s+/g, " ").trim().toLowerCase();

  // A row stays when every searched column contains its value. "13000000" also finds
  // 13.000.000, and a date column also takes 09/11/2026 for 09 Nov 2026.
  function cellMatches(cell, term) {
    const text = normalize(cell?.textContent ?? "");
    if (text.includes(term)) return true;
    if (/^\d+$/.test(term) && text.replace(/\./g, "").includes(term)) return true;
    const date = parseDate(text);
    const wanted = parseDate(term);
    return date !== null && wanted !== null && date === wanted;
  }

  function applyFilters(table) {
    const filters = table.columnFilters;
    const body = table.tBodies[0];
    let shown = 0;
    for (const row of body.rows) {
      if (row.dataset.filterEmpty) continue;
      const keep = [...filters].every(([index, term]) => cellMatches(row.cells[index], term));
      row.hidden = !keep;
      if (keep) shown++;
    }

    let empty = body.querySelector("tr[data-filter-empty]");
    if (shown === 0 && filters.size > 0) {
      if (!empty) {
        empty = body.insertRow();
        empty.dataset.filterEmpty = "true";
        const cell = empty.insertCell();
        cell.colSpan = table.tHead.rows[0].cells.length;
        cell.className = "text-center text-muted py-4";
        cell.textContent = "Tidak ada data yang cocok dengan pencarian kolom.";
      }
    } else {
      empty?.remove();
    }
  }

  function filterBy(table, button, index, value) {
    const term = normalize(value);
    if (term) table.columnFilters.set(index, term);
    else table.columnFilters.delete(index);

    if (term) button.dataset.filterValue = value;
    else delete button.dataset.filterValue;
    button.querySelector(".column-filter-mark")?.remove();
    if (term) {
      const mark = document.createElement("span");
      mark.className = "column-filter-mark";
      mark.setAttribute("aria-hidden", "true");
      button.append(mark);
    }
    applyFilters(table);
  }

  function columnKind(table, index) {
    const first = [...table.tBodies[0].rows].map((row) => sortValue(row.cells[index])).find((v) => !v.empty);
    if (first?.date != null) return "date";
    return first?.number != null ? "number" : "text";
  }

  function canSort(table) {
    if (table.dataset.sortReady || table.hasAttribute("data-no-sort")) return false;
    if (table.tHead?.rows.length !== 1 || table.tBodies.length !== 1) return false;
    if (table.querySelector(".column-header")) return false;

    const body = table.tBodies[0];
    if (body.rows.length < 2) return false;
    if (body.querySelector("[rowspan], [colspan], input:not([type=hidden]), select, textarea")) return false;
    return true;
  }

  function enhance(table) {
    if (!canSort(table)) return;
    table.dataset.sortReady = "true";
    table.columnFilters = new Map();

    [...table.tHead.rows[0].cells].forEach((header, index) => {
      const label = header.textContent.replace(/\s+/g, " ").trim().toLowerCase();
      if (NON_DATA_HEADERS.has(label) || header.colSpan > 1) return;

      const text = document.createElement("span");
      text.className = "column-header-label";
      text.append(...header.childNodes);

      const button = document.createElement("button");
      button.type = "button";
      button.className = "column-header";
      button.dataset.kind = columnKind(table, index);
      button.setAttribute("aria-haspopup", "dialog");
      button.setAttribute("aria-expanded", "false");
      button.append(text);
      button.dataset.filterKey = String(index);
      button.dataset.filterClient = "true";
      button.addEventListener("column-sort", (event) => sortBy(table, button, event.detail.descending));
      button.addEventListener("column-filter", (event) => filterBy(table, button, index, event.detail.value));
      header.append(button);
    });
  }

  function enhanceAll() {
    document.querySelectorAll("table").forEach(enhance);
  }

  document.addEventListener("DOMContentLoaded", enhanceAll);
  document.body.addEventListener("htmx:afterSettle", enhanceAll);
})();
