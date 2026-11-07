// Click-to-sort for tables without paging. Paged lists sort on the server instead, through
// <th sort-key>. Skipped: tables with data-no-sort, form fields, rowspans, or spanning rows.
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
      button.addEventListener("column-sort", (event) => sortBy(table, button, event.detail.descending));
      header.append(button);
    });
  }

  function enhanceAll() {
    document.querySelectorAll("table").forEach(enhance);
  }

  document.addEventListener("DOMContentLoaded", enhanceAll);
  document.body.addEventListener("htmx:afterSettle", enhanceAll);
})();
