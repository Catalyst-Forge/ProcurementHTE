// The card behind every sortable column header. Server-sorted headers carry their URLs
// (SortableHeaderTagHelper); browser-sorted ones get a column-sort event (table-sort.js).
(function () {
  "use strict";

  const SORT_LABELS = {
    text: ["Urutkan A–Z", "Urutkan Z–A"],
    date: ["Terlama dulu", "Terbaru dulu"],
    number: ["Terkecil dulu", "Terbesar dulu"],
  };
  const FILTER_PREFIX = "f.";
  const GAP = 4;
  const EDGE = 8;

  let menu = null;
  let trigger = null;

  const headerLabel = (button) => button.querySelector(".column-header-label").textContent.trim();

  // A real link inside #app-content, so navigation goes through the page's htmx boost.
  function navigate(url) {
    const link = document.createElement("a");
    link.href = url;
    link.hidden = true;
    (document.getElementById("app-content") ?? document.body).append(link);
    window.htmx?.process(link);
    link.click();
  }

  function urlWithFilters(change) {
    const params = new URLSearchParams(window.location.search);
    change(params);
    params.delete("page");
    return `?${params}`;
  }

  const urlForFilter = (key, value) =>
    urlWithFilters((params) => {
      if (value) params.set(FILTER_PREFIX + key, value);
      else params.delete(FILTER_PREFIX + key);
    });

  const urlWithoutFilters = () =>
    urlWithFilters((params) => {
      [...params.keys()].filter((key) => key.startsWith(FILTER_PREFIX)).forEach((key) => params.delete(key));
    });

  function closeMenu({ restoreFocus = true } = {}) {
    if (!menu) return;
    menu.remove();
    menu = null;
    trigger.setAttribute("aria-expanded", "false");
    if (restoreFocus) trigger.focus();
    trigger = null;
  }

  function applySort(button, descending) {
    closeMenu();
    const url = descending ? button.dataset.sortDesc : button.dataset.sortAsc;
    if (url) navigate(url);
    else button.dispatchEvent(new CustomEvent("column-sort", { detail: { descending } }));
  }

  function sortItem(button, label, descending) {
    const item = document.createElement("button");
    item.type = "button";
    item.className = "column-menu-item";
    item.textContent = label;

    if (button.dataset.sorted === (descending ? "desc" : "asc")) {
      item.setAttribute("aria-current", "true");
      const check = document.createElement("i");
      check.className = "bi bi-check2";
      check.setAttribute("aria-hidden", "true");
      item.append(check);
    }

    item.addEventListener("click", () => applySort(button, descending));
    return item;
  }

  function searchField(button) {
    const box = document.createElement("form");
    box.className = "column-menu-search";

    const label = document.createElement("label");
    label.htmlFor = "columnMenuSearch";
    label.textContent = `Cari di ${headerLabel(button)}`;

    const input = document.createElement("input");
    input.id = "columnMenuSearch";
    input.type = "search";
    input.className = "form-control form-control-sm";
    input.value = button.dataset.filterValue ?? "";
    input.autocomplete = "off";

    box.append(label, input);
    box.addEventListener("submit", (event) => {
      event.preventDefault();
      const url = urlForFilter(button.dataset.filterKey, input.value.trim());
      closeMenu();
      navigate(url);
    });

    if (button.dataset.filterValue) {
      const clear = document.createElement("button");
      clear.type = "button";
      clear.className = "column-menu-clear";
      clear.textContent = "Hapus filter";
      clear.addEventListener("click", () => {
        const url = urlForFilter(button.dataset.filterKey, "");
        closeMenu();
        navigate(url);
      });
      box.append(clear);
    }
    return box;
  }

  function place(button) {
    const anchor = button.getBoundingClientRect();
    const { width, height } = menu.getBoundingClientRect();
    const below = anchor.bottom + GAP;
    const top = below + height > window.innerHeight - EDGE ? anchor.top - height - GAP : below;
    const left = Math.min(anchor.left, window.innerWidth - width - EDGE);

    menu.style.top = `${Math.max(EDGE, top)}px`;
    menu.style.left = `${Math.max(EDGE, left)}px`;
  }

  function openMenu(button) {
    const [ascLabel, descLabel] = SORT_LABELS[button.dataset.kind] ?? SORT_LABELS.text;

    menu = document.createElement("div");
    menu.className = "column-menu";
    menu.setAttribute("role", "dialog");
    menu.setAttribute("aria-label", `Menu kolom ${headerLabel(button)}`);
    menu.append(sortItem(button, ascLabel, false), sortItem(button, descLabel, true));
    if (button.dataset.filterKey) menu.append(searchField(button));

    document.body.append(menu);
    trigger = button;
    trigger.setAttribute("aria-expanded", "true");
    place(button);
    (menu.querySelector("input") ?? menu.querySelector("button")).focus();
  }

  function renderChips() {
    const box = document.querySelector("[data-column-filter-chips]");
    if (!box) return;

    const active = [...document.querySelectorAll(".column-header[data-filter-value]")].filter(
      (button) => button.dataset.filterValue
    );
    box.replaceChildren();
    box.hidden = active.length === 0;
    if (!active.length) return;

    for (const button of active) {
      const label = headerLabel(button);
      const chip = document.createElement("a");
      chip.className = "column-filter-chip";
      chip.href = urlForFilter(button.dataset.filterKey, "");
      chip.setAttribute("aria-label", `Hapus filter ${label}`);
      chip.textContent = `${label}: ${button.dataset.filterValue}`;

      const remove = document.createElement("i");
      remove.className = "bi bi-x";
      remove.setAttribute("aria-hidden", "true");
      chip.append(remove);
      box.append(chip);
    }

    const clearAll = document.createElement("a");
    clearAll.href = urlWithoutFilters();
    clearAll.textContent = "Hapus semua";
    box.append(clearAll);
    window.htmx?.process(box);
  }

  document.addEventListener("click", (event) => {
    const button = event.target.closest?.(".column-header");
    if (button) {
      const wasOpen = trigger === button;
      closeMenu({ restoreFocus: false });
      if (!wasOpen) openMenu(button);
      return;
    }
    if (menu && !menu.contains(event.target)) closeMenu({ restoreFocus: false });
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeMenu();
  });

  window.addEventListener("resize", () => closeMenu({ restoreFocus: false }));
  document.addEventListener(
    "scroll",
    (event) => {
      if (menu && !menu.contains(event.target)) closeMenu({ restoreFocus: false });
    },
    true
  );

  document.addEventListener("DOMContentLoaded", renderChips);
  document.body.addEventListener("htmx:afterSettle", renderChips);
  document.body.addEventListener("htmx:beforeRequest", () => closeMenu({ restoreFocus: false }));
})();
