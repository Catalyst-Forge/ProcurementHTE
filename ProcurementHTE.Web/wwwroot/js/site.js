(function () {
  "use strict";

  if ("scrollRestoration" in history) {
    history.scrollRestoration = "manual";
  }

  const SELECTORS = {
    navLink: "[data-nav-controller]",
    collapse: "[data-nav-collapse]"
  };

  const parseList = (value) =>
    (value || "")
      .split(",")
      .map((item) => item.trim().toLowerCase())
      .filter(Boolean);

  function toggleChevron(collapseId, shouldShow) {
    if (!collapseId) {
      return;
    }

    const selector = [`[href="#${collapseId}"]`, `[data-bs-target="#${collapseId}"]`].join(", ");
    document.querySelectorAll(selector).forEach((toggle) => {
      toggle.setAttribute("aria-expanded", shouldShow ? "true" : "false");
      const chevron = toggle.querySelector(".bi-chevron-down");
      if (chevron) {
        chevron.classList.toggle("rotate", shouldShow);
      }
    });
  }

  function applyRouteState(controller, action) {
    const normalizedController = (controller || "").toLowerCase();
    const normalizedAction = (action || "").toLowerCase();

    updateHxBoost(normalizedController, normalizedAction);

    document.querySelectorAll(SELECTORS.navLink).forEach((link) => {
      const controllers = parseList(link.getAttribute("data-nav-controller"));
      const actions = parseList(link.getAttribute("data-nav-action"));
      const controllerMatch = controllers.length === 0 || controllers.includes(normalizedController);
      const actionMatch = actions.length === 0 || actions.includes(normalizedAction);
      link.classList.toggle("active", controllerMatch && actionMatch);
    });

    document.querySelectorAll(SELECTORS.collapse).forEach((section) => {
      const controllers = parseList(section.getAttribute("data-nav-collapse"));
      const shouldShow = controllers.length > 0 && controllers.includes(normalizedController);
      section.classList.toggle("show", shouldShow);
      toggleChevron(section.id, shouldShow);
    });
  }

  const updateHxBoost = (controller, action) => {
    const main = document.getElementById("app-content");
    if (!main) return;
    const heavyPnL =
      controller === "procurements" &&
      (action === "createprofitloss" || action === "editprofitloss");
    main.setAttribute("hx-boost", heavyPnL ? "false" : "true");
  };

  function scrollContentToTop(target) {
    const scrollTarget = target || document.querySelector("#app-content");
    let attempts = 0;
    const maxAttempts = 5;

    const scrollToTop = () => {
      attempts += 1;

      if (scrollTarget && typeof scrollTarget.scrollTo === "function") {
        scrollTarget.scrollTo({ top: 0, left: 0 });
      } else if (scrollTarget) {
        scrollTarget.scrollTop = 0;
      }

      window.scrollTo({ top: 0, left: 0 });
      document.documentElement.scrollTop = 0;
      document.body.scrollTop = 0;

      if (attempts < maxAttempts) {
        requestAnimationFrame(scrollToTop);
      }
    };

    requestAnimationFrame(scrollToTop);
  }

  function executeScripts(scope) {
    if (!scope) {
      return;
    }

    scope.querySelectorAll("script").forEach((script) => {
      const type = (script.type || "").trim();
      const isModule = type === "module";
      const executable = !type || type === "text/javascript" || isModule;
      if (!executable) {
        return;
      }

      const newScript = document.createElement("script");
      if (type) {
        newScript.type = type;
      }

      script.getAttributeNames().forEach((attr) => {
        if (attr === "src" || attr === "type") {
          return;
        }
        const value = script.getAttribute(attr);
        if (value !== null) {
          newScript.setAttribute(attr, value);
        }
      });

      if (script.src) {
        newScript.src = script.src;
      } else {
        newScript.textContent = script.textContent;
      }

      script.replaceWith(newScript);
    });
  }

  function syncFromPartialRoot(root, target) {
    if (!root) {
      return;
    }

    const activePage = root.getAttribute("data-partial-active-page");
    const controller = root.getAttribute("data-partial-controller") || "";
    const action = root.getAttribute("data-partial-action") || "";
    const documentTitle = root.getAttribute("data-partial-document-title");

    if (activePage !== null) {
      document.body.setAttribute("data-active-page", activePage);
    }

    document.body.setAttribute("data-active-controller", controller);
    document.body.setAttribute("data-active-action", action);
    if (documentTitle) {
      document.title = documentTitle;
    }
    applyRouteState(controller, action);
    scrollContentToTop(target);
  }

  document.body.addEventListener("htmx:afterSwap", (event) => {
    const target = event.detail?.target;
    if (!target) {
      return;
    }
    const shouldSelfExecuteScripts =
      window.htmx && window.htmx.config && window.htmx.config.allowScriptTags === false;
    if (shouldSelfExecuteScripts) {
      executeScripts(target);
    }
    const root = target.querySelector("[data-partial-root]");
    if (root) {
      syncFromPartialRoot(root, target);
    } else {
      scrollContentToTop(target);
      // When a partial does not include route metadata (e.g., full-page load),
      // still update hx-boost based on body data attributes.
      const bodyController = (document.body.dataset.activeController || "").toLowerCase();
      const bodyAction = (document.body.dataset.activeAction || "").toLowerCase();
      updateHxBoost(bodyController, bodyAction);
    }
  });

  document.body.addEventListener("htmx:beforeSwap", (event) => {
    const status = event.detail?.xhr?.status;
    if (status === 401) {
      window.location.reload();
    }
  });

  document.body.addEventListener("htmx:historyRestore", (event) => {
    const fragment = event.detail?.item;
    const target = document.querySelector("#app-content");
    if (fragment && typeof fragment.querySelector === "function") {
      const root = fragment.querySelector("[data-partial-root]");
      if (root) {
        syncFromPartialRoot(root, target);
        return;
      }
    }
    scrollContentToTop(target);
  });

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

  const loadingOverlay = document.getElementById("global-loading-overlay");
  let overlayStart = 0;
  let overlayTimer = null;
  let overlayFailsafe = null;
  let overlaySuppressUntil = 0;

  const hideOverlayNow = () => {
    if (!loadingOverlay) {
      return;
    }
    loadingOverlay.classList.add("d-none");
    loadingOverlay.dataset.visible = "false";
  };

  const suppressOverlay = (ms = 0) => {
    overlaySuppressUntil = performance.now() + ms;
    hideGlobalLoadingOverlay(true);
  };

  const isOverlaySuppressed = () => performance.now() < overlaySuppressUntil;

  function showGlobalLoadingOverlay() {
    if (!loadingOverlay || loadingOverlay.dataset.visible === "true") {
      return;
    }
    overlaySuppressUntil = 0;
    overlayStart = performance.now();
    loadingOverlay.dataset.visible = "true";
    loadingOverlay.classList.remove("d-none");
    clearTimeout(overlayFailsafe);
    overlayFailsafe = setTimeout(() => {
      hideGlobalLoadingOverlay(true);
    }, 15000);
  }

  function hideGlobalLoadingOverlay(force = false) {
    if (!loadingOverlay || loadingOverlay.dataset.visible !== "true") {
      return;
    }
    const elapsed = performance.now() - overlayStart;
    const delay = force ? 0 : Math.max(0, 1000 - elapsed);
    clearTimeout(overlayTimer);
    clearTimeout(overlayFailsafe);
    overlayFailsafe = null;
    if (delay === 0) {
      hideOverlayNow();
      return;
    }
    overlayTimer = setTimeout(() => {
      hideOverlayNow();
    }, delay);
  }

  window.AppLoadingOverlay = {
    show: showGlobalLoadingOverlay,
    hide: hideGlobalLoadingOverlay,
  };

  const isBackForwardNavigation = () => {
    const nav = performance.getEntriesByType("navigation");
    const last = nav && nav.length ? nav[nav.length - 1] : null;
    return last && last.type === "back_forward";
  };

  window.addEventListener("pageshow", (event) => {
    if (event.persisted || isBackForwardNavigation()) {
      suppressOverlay(750);
    }
  });
  window.addEventListener("popstate", () => {
    suppressOverlay(750);
  });
  window.addEventListener("beforeunload", () => {
    // Keep overlay visible when we're intentionally navigating after a submit;
    // only suppress if nothing is showing.
    if (loadingOverlay?.dataset?.visible !== "true") {
      suppressOverlay(0);
    }
  });
  // Avoid persisting a visible overlay into the history snapshot.
  document.body.addEventListener("htmx:beforeHistorySave", () => {
    hideOverlayNow();
  });
  document.body.addEventListener("htmx:historyRestore", () => {
    suppressOverlay(750);
  });

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
    if (!form) {
      return false;
    }
    const wantsOverlay =
      form.classList.contains("js-loading-form") ||
      form.dataset.loadingOverlay === "true" ||
      form.hasAttribute("data-loading-overlay");
    if (!wantsOverlay) {
      return false;
    }
    // For delete forms, only show overlay after user confirmed.
    if (form.classList.contains("delete-form") && form.dataset.confirming !== "true") {
      return false;
    }
    return true;
  };

  document.addEventListener(
    "submit",
    (event) => {
      const target = event.target;
      const form =
        target instanceof HTMLFormElement
          ? target
          : target && typeof target.closest === "function"
            ? target.closest("form")
            : null;
      if (!form || !shouldApplyLoadingOverlay(form)) {
        return;
      }

      if (form.dataset.loadingActive === "true") {
        return;
      }

      showGlobalLoadingOverlay();
      form.dataset.loadingActive = "true";
      requestAnimationFrame(() => {
        if (event.defaultPrevented && formHasClientErrors(form)) {
          form.dataset.loadingActive = "false";
          hideGlobalLoadingOverlay();
        }
      });
    },
    true
  );

  const findLoadingFormFromSource = (source) => {
    if (!source) {
      return null;
    }
    if (source instanceof HTMLFormElement && shouldApplyLoadingOverlay(source)) {
      return source;
    }
    if (typeof source.closest === "function") {
      const form = source.closest("form");
      return shouldApplyLoadingOverlay(form) ? form : null;
    }
    return null;
  };

  const isWithinMainContent = (element) => {
    if (!element) {
      return false;
    }
    if (element.id === "app-content") {
      return true;
    }
    if (typeof element.closest === "function") {
      return Boolean(element.closest("#app-content"));
    }
    return false;
  };

  const shouldShowOverlayForHtmx = (source, target) => {
    if (
      source &&
      typeof source.closest === "function" &&
      (source.closest(".delete-form") || source.closest("[data-no-loading-overlay]"))
    ) {
      return false;
    }
    return isWithinMainContent(target);
  };

  const hxLoadingRequests = new WeakMap();

  const isHistoryRestoreRequest = (eventDetail) => {
    const headers =
      eventDetail?.headers ||
      eventDetail?.requestConfig?.headers ||
      eventDetail?.xhrConfig?.headers;
    if (!headers) {
      return false;
    }
    const flag =
      headers["HX-History-Restore-Request"] ??
      headers["hx-history-restore-request"] ??
      headers["Hx-History-Restore-Request"];
    return String(flag).toLowerCase() === "true";
  };

  document.body.addEventListener("htmx:beforeRequest", (event) => {
    const xhr = event.detail?.xhr;
    if (!xhr) {
      return;
    }

    if (isOverlaySuppressed()) {
      return;
    }

    if (isHistoryRestoreRequest(event.detail)) {
      hideGlobalLoadingOverlay();
      return;
    }

    const source = event.detail?.elt || null;
    const target = event.detail?.target || null;
    const form = findLoadingFormFromSource(source);

    if (form) {
      const wasActive = form.dataset.loadingActive === "true";
      form.dataset.loadingActive = "true";

      if (!wasActive) {
        showGlobalLoadingOverlay();
      }

      hxLoadingRequests.set(xhr, { form, overlayShown: true });
      return;
    }

    const overlayNeeded = shouldShowOverlayForHtmx(source, target);
    if (overlayNeeded) {
      showGlobalLoadingOverlay();
    }

    hxLoadingRequests.set(xhr, { overlayShown: overlayNeeded });
  });

  const releaseHxOverlay = (event) => {
    const xhr = event.detail?.xhr;
    if (!xhr) {
      return;
    }

    const meta = hxLoadingRequests.get(xhr);
    if (!meta) {
      return;
    }
    hxLoadingRequests.delete(xhr);

    if (meta.form && meta.form.dataset) {
      meta.form.dataset.loadingActive = "false";
    }

    if (meta.overlayShown) {
      hideGlobalLoadingOverlay();
    }
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

  document.addEventListener("submit", (event) => {
    const target = event.target;
    const form =
      target instanceof HTMLFormElement
        ? target
        : target && typeof target.closest === "function"
          ? target.closest("form")
          : null;
    if (!form || !form.classList.contains("delete-form") || form.dataset.confirming === "true") {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    if (typeof event.stopImmediatePropagation === "function") {
      event.stopImmediatePropagation();
    }
    if (form.dataset.confirmPending === "true") {
      return;
    }
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
    })
      .then((confirmed) => {
        form.dataset.confirmPending = "false";
        if (confirmed) {
          form.dataset.confirming = "true";
          if (typeof form.requestSubmit === "function") {
            form.requestSubmit();
          } else {
            form.submit();
          }
        }
      })
      .catch(() => {
        form.dataset.confirmPending = "false";
      });
  }, true);
})();
