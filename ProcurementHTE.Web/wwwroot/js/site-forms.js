(function () {
  "use strict";

  const DEFAULT_CONFIRM = {
    title: "Delete this record?",
    text: "This action cannot be undone.",
    confirmButtonText: "Yes, delete",
    cancelButtonText: "Cancel",
    icon: "warning",
  };

  const decodeHtml = (value) => {
    if (!value) return value;
    const textarea = document.createElement("textarea");
    textarea.innerHTML = value;
    return textarea.value;
  };

  const formHasClientErrors = (form) => {
    if (!form) return false;
    if (form.querySelector(".input-validation-error")) return true;
    if (form.querySelector(".field-validation-error:not(.field-validation-valid)")) return true;
    try {
      if (form.querySelector(":invalid")) return true;
    } catch {
      /* ignore */
    }
    return false;
  };

  const shouldApplyLoadingOverlay = (form) => {
    if (!form) return false;
    const wantsOverlay =
      form.classList.contains("js-loading-form") ||
      form.dataset.loadingOverlay === "true" ||
      form.hasAttribute("data-loading-overlay");
    if (!wantsOverlay) return false;
    if (form.classList.contains("delete-form") && form.dataset.confirming !== "true") return false;
    return true;
  };

  document.addEventListener("submit", (event) => {
      const target = event.target;
      const form = target instanceof HTMLFormElement ? target : (target && typeof target.closest === "function" ? target.closest("form") : null);
      
      // Delete confirm logic
      if (form && form.classList.contains("delete-form") && form.dataset.confirming !== "true") {
        event.preventDefault();
        event.stopPropagation();
        if (typeof event.stopImmediatePropagation === "function") event.stopImmediatePropagation();
        
        if (form.dataset.confirmPending === "true") return;
        form.dataset.confirmPending = "true";

        const title = decodeHtml(form.dataset.confirmTitle) || DEFAULT_CONFIRM.title;
        const message = decodeHtml(form.dataset.confirmMessage) || DEFAULT_CONFIRM.text;
        const html = decodeHtml(form.dataset.confirmHtml);
        const confirmText = form.dataset.confirmConfirm || "Delete";
        const cancelText = form.dataset.confirmCancel || DEFAULT_CONFIRM.cancelButtonText;

        confirmAction({
          title,
          text: html ? undefined : message,
          html,
          confirmButtonText: confirmText,
          cancelButtonText: cancelText,
          confirmButtonColor: form.dataset.confirmConfirmColor,
          cancelButtonColor: form.dataset.confirmCancelColor,
        }).then((confirmed) => {
          form.dataset.confirmPending = "false";
          if (confirmed) {
            form.dataset.confirming = "true";
            if (typeof form.requestSubmit === "function") form.requestSubmit();
            else form.submit();
          }
        }).catch(() => {
          form.dataset.confirmPending = "false";
        });
        return; // Don't proceed to loading overlay logic until confirmed
      }

      // Loading overlay logic
      if (!form || !shouldApplyLoadingOverlay(form)) return;
      if (form.dataset.loadingActive === "true") return;

      window.SiteApp.showGlobalLoadingOverlay();
      form.dataset.loadingActive = "true";
      requestAnimationFrame(() => {
        if (event.defaultPrevented && formHasClientErrors(form)) {
          form.dataset.loadingActive = "false";
          window.SiteApp.hideGlobalLoadingOverlay();
        }
      });
    },
    true
  );

  const findLoadingFormFromSource = (source) => {
    if (!source) return null;
    if (source instanceof HTMLFormElement && shouldApplyLoadingOverlay(source)) return source;
    if (typeof source.closest === "function") {
      const form = source.closest("form");
      return shouldApplyLoadingOverlay(form) ? form : null;
    }
    return null;
  };

  const isWithinMainContent = (element) => {
    if (!element) return false;
    if (element.id === "app-content") return true;
    if (typeof element.closest === "function") return Boolean(element.closest("#app-content"));
    return false;
  };

  const shouldShowOverlayForHtmx = (source, target) => {
    if (source && typeof source.closest === "function" && (source.closest(".delete-form") || source.closest("[data-no-loading-overlay]"))) {
      return false;
    }
    return isWithinMainContent(target);
  };

  const hxLoadingRequests = new WeakMap();

  const isHistoryRestoreRequest = (eventDetail) => {
    const headers = eventDetail?.headers || eventDetail?.requestConfig?.headers || eventDetail?.xhrConfig?.headers;
    if (!headers) return false;
    const flag = headers["HX-History-Restore-Request"] ?? headers["hx-history-restore-request"] ?? headers["Hx-History-Restore-Request"];
    return String(flag).toLowerCase() === "true";
  };

  document.body.addEventListener("htmx:beforeRequest", (event) => {
    const xhr = event.detail?.xhr;
    if (!xhr) return;

    if (window.SiteApp.isOverlaySuppressed && window.SiteApp.isOverlaySuppressed()) return;

    if (isHistoryRestoreRequest(event.detail)) {
      window.SiteApp.hideGlobalLoadingOverlay();
      return;
    }

    const source = event.detail?.elt || null;
    const target = event.detail?.target || null;
    const form = findLoadingFormFromSource(source);

    if (form) {
      const wasActive = form.dataset.loadingActive === "true";
      form.dataset.loadingActive = "true";
      if (!wasActive) window.SiteApp.showGlobalLoadingOverlay();
      hxLoadingRequests.set(xhr, { form, overlayShown: true });
      return;
    }

    const overlayNeeded = shouldShowOverlayForHtmx(source, target);
    if (overlayNeeded) window.SiteApp.showGlobalLoadingOverlay();

    hxLoadingRequests.set(xhr, { overlayShown: overlayNeeded });
  });

  const releaseHxOverlay = (event) => {
    const xhr = event.detail?.xhr;
    if (!xhr) return;

    const meta = hxLoadingRequests.get(xhr);
    if (!meta) return;
    hxLoadingRequests.delete(xhr);

    if (meta.form && meta.form.dataset) meta.form.dataset.loadingActive = "false";
    if (meta.overlayShown) window.SiteApp.hideGlobalLoadingOverlay();
  };

  ["htmx:afterSwap", "htmx:afterOnLoad", "htmx:responseError", "htmx:sendError", "htmx:timeout", "htmx:abort", "htmx:swapError"].forEach((evtName) => {
    document.body.addEventListener(evtName, releaseHxOverlay);
  });

  async function confirmAction(options = {}) {
    const config = { ...DEFAULT_CONFIRM, ...options };
    const hasSwal = typeof window !== "undefined" && window.Swal && typeof window.Swal.fire === "function";

    if (hasSwal) {
      const result = await window.Swal.fire({
        icon: config.icon,
        title: config.title,
        text: config.html ? undefined : config.text,
        html: config.html,
        showCancelButton: true,
        confirmButtonText: config.confirmButtonText,
        cancelButtonText: config.cancelButtonText,
        confirmButtonColor: config.confirmButtonColor || "#dc3545",
        cancelButtonColor: config.cancelButtonColor || "#6c757d",
        focusCancel: true,
      });
      return result.isConfirmed;
    }
    return window.confirm(config.text || config.title || DEFAULT_CONFIRM.text);
  }
})();
