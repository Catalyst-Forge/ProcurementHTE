// The month and year picker above a job list (Views/Shared/Components/PeriodFilter): picking either
// submits its form at once, back on page 1. Without JavaScript the form's Terapkan button does it.
(function () {
  "use strict";

  document.addEventListener("change", (event) => {
    const select = event.target.closest?.("[data-period-select]");
    const form = select?.form;
    if (!form) return;

    if (form.elements.page) form.elements.page.value = 1;
    if (typeof form.requestSubmit === "function") form.requestSubmit();
    else form.submit();
  });
})();
