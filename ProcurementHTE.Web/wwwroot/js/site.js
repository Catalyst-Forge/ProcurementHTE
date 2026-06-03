(function () {
  "use strict";

  // Prevent duplicate initialization after HTMX navigation
  if (window.__siteJsInitialized) {
    console.log('[site.js] Already initialized, skipping');
    return;
  }
  window.__siteJsInitialized = true;

  if ("scrollRestoration" in history) {
    history.scrollRestoration = "manual";
  }

  // Internal state tracking - completely independent from DOM/Bootstrap
  const dropdownStates = new WeakMap();
  const collapseStates = new WeakMap();

  // Disable Bootstrap's data-api for dropdowns and collapses
  // This prevents Bootstrap from interfering with our manual handling
  function disableBootstrapDataApi() {
    // Initialize dropdown states
    document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(el => {
      if (!el.hasAttribute('data-manual-handled')) {
        el.setAttribute('data-manual-handled', 'dropdown');
        // Initialize state as closed
        dropdownStates.set(el, false);
        // Reset DOM to match
        el.classList.remove('show');
        el.setAttribute('aria-expanded', 'false');
        const menu = el.nextElementSibling?.classList.contains('dropdown-menu') 
          ? el.nextElementSibling 
          : el.closest('.dropdown, .nav-item, .btn-group')?.querySelector('.dropdown-menu');
        if (menu) {
          menu.classList.remove('show');
        }
      }
    });
    
    // Initialize collapse states based on current DOM
    document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(el => {
      if (!el.hasAttribute('data-manual-handled')) {
        el.setAttribute('data-manual-handled', 'collapse');
        const targetSelector = el.getAttribute('href') || el.getAttribute('data-bs-target');
        if (targetSelector) {
          const collapseElement = document.querySelector(targetSelector);
          if (collapseElement) {
            collapseElement.classList.remove('collapsing');
            // Initialize state based on current DOM
            const isShown = collapseElement.classList.contains('show');
            collapseStates.set(collapseElement, isShown);
          }
        }
      }
    });
  }

  // Run on initial load
  disableBootstrapDataApi();

  // Re-run after HTMX swaps content
  document.body.addEventListener('htmx:afterSwap', disableBootstrapDataApi);
  document.body.addEventListener('htmx:afterSettle', disableBootstrapDataApi);

  // Single click handler for both dropdown and collapse
  // Using capture phase AND stopImmediatePropagation to beat Bootstrap
  document.addEventListener('click', function(e) {
    
    // Handle dropdown toggle clicks
    const dropdownToggle = e.target.closest('[data-bs-toggle="dropdown"]');
    if (dropdownToggle) {
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
      
      // Find the dropdown menu
      let dropdownMenu = dropdownToggle.nextElementSibling?.classList.contains('dropdown-menu') 
        ? dropdownToggle.nextElementSibling 
        : null;
      
      if (!dropdownMenu) {
        const parent = dropdownToggle.closest('.dropdown, .nav-item, .btn-group');
        if (parent) dropdownMenu = parent.querySelector('.dropdown-menu');
      }
      
      if (!dropdownMenu && dropdownToggle.id) {
        dropdownMenu = document.querySelector(`.dropdown-menu[aria-labelledby="${dropdownToggle.id}"]`);
      }
      
      if (!dropdownMenu) {
        return false;
      }
      
      // Use WeakMap for state tracking - completely independent from DOM
      let wasShown = dropdownStates.get(dropdownToggle);
      if (wasShown === undefined) {
        // First time seeing this toggle, initialize as closed
        wasShown = false;
        dropdownStates.set(dropdownToggle, false);
      }
      
      // Close all other dropdowns first
      document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
        if (menu !== dropdownMenu) {
          menu.classList.remove('show');
          const menuParent = menu.closest('.dropdown, .nav-item, .btn-group');
          const menuToggle = menuParent?.querySelector('[data-bs-toggle="dropdown"]') || menu.previousElementSibling;
          if (menuToggle) {
            menuToggle.classList.remove('show');
            menuToggle.setAttribute('aria-expanded', 'false');
            dropdownStates.set(menuToggle, false);
          }
        }
      });
      
      if (!wasShown) {
        dropdownToggle.classList.add('show');
        dropdownToggle.setAttribute('aria-expanded', 'true');
        dropdownMenu.classList.add('show');
        dropdownMenu.setAttribute('data-bs-popper', 'static');
        dropdownStates.set(dropdownToggle, true);
      } else {
        dropdownToggle.classList.remove('show');
        dropdownToggle.setAttribute('aria-expanded', 'false');
        dropdownMenu.classList.remove('show');
        dropdownStates.set(dropdownToggle, false);
      }
      
      return false;
    }
    
    // Handle collapse toggle clicks  
    const collapseToggle = e.target.closest('[data-bs-toggle="collapse"]');
    if (collapseToggle) {
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
      
      const targetSelector = collapseToggle.getAttribute('href') || collapseToggle.getAttribute('data-bs-target');
      if (!targetSelector) {
        return false;
      }
      
      const collapseElement = document.querySelector(targetSelector);
      if (!collapseElement) {
        return false;
      }
      
      // Remove any stuck transitional state from Bootstrap
      collapseElement.classList.remove('collapsing');
      
      // Use WeakMap for state tracking - completely independent from DOM
      let wasShown = collapseStates.get(collapseElement);
      if (wasShown === undefined) {
        // First time seeing this element, check DOM for initial state
        wasShown = collapseElement.classList.contains('show');
        collapseStates.set(collapseElement, wasShown);
      }
      
      // Find chevron
      const chevron = collapseToggle.querySelector('i.bi-chevron-down, i.bi-chevron-up, .bi.bi-chevron-down, .bi.bi-chevron-up');
      
      if (!wasShown) {
        // Show it
        collapseElement.classList.remove('collapse');
        collapseElement.classList.add('collapse', 'show');
        collapseElement.style.height = 'auto';
        collapseStates.set(collapseElement, true);
        collapseToggle.setAttribute('aria-expanded', 'true');
        collapseToggle.classList.remove('collapsed');
        if (chevron) {
          chevron.classList.remove('bi-chevron-down');
          chevron.classList.add('bi-chevron-up', 'rotate');
        }
      } else {
        // Hide it
        collapseElement.classList.add('collapse');
        collapseElement.classList.remove('show');
        collapseElement.style.height = '';
        collapseStates.set(collapseElement, false);
        collapseToggle.setAttribute('aria-expanded', 'false');
        collapseToggle.classList.add('collapsed');
        if (chevron) {
          chevron.classList.remove('bi-chevron-up', 'rotate');
          chevron.classList.add('bi-chevron-down');
        }
      }
      
      return false;
    }
  }, true); // Capture phase - runs before Bootstrap's handler
  
  // Handle click outside to close dropdowns
  document.addEventListener('click', function(e) {
    // If click is not on a dropdown or btn-group or nav-item dropdown, close all open dropdowns
    if (!e.target.closest('.dropdown, .nav-item.dropdown, .btn-group, [data-bs-toggle="dropdown"], .dropdown-menu')) {
      document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
        menu.classList.remove('show');
        const menuParent = menu.closest('.dropdown, .nav-item, .btn-group');
        const toggle = menuParent?.querySelector('[data-bs-toggle="dropdown"]') || menu.previousElementSibling;
        if (toggle) {
          toggle.classList.remove('show');
          toggle.setAttribute('aria-expanded', 'false');
          dropdownStates.set(toggle, false);
        }
      });
    }
  });

  const SELECTORS = {
    navLink: "[data-nav-controller]",
    collapse: "[data-nav-collapse]",
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
      // Find chevron with both possible selector patterns
      const chevron = toggle.querySelector("i.bi-chevron-down, i.bi-chevron-up, .bi.bi-chevron-down, .bi.bi-chevron-up");
      if (chevron) {
        if (shouldShow) {
          chevron.classList.remove("bi-chevron-down");
          chevron.classList.add("bi-chevron-up", "rotate");
        } else {
          chevron.classList.remove("bi-chevron-up", "rotate");
          chevron.classList.add("bi-chevron-down");
        }
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
    const heavyPnL = controller === "procurements" && (action === "createprofitloss" || action === "editprofitloss");
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
    
    // Normal HTMX navigation works fine with our handlers
    
    const shouldSelfExecuteScripts = window.htmx && window.htmx.config && window.htmx.config.allowScriptTags === false;
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
    
    // Reinitialize Bootstrap dropdowns after HTMX swap
    reinitializeBootstrapComponents();
  });

  // Function to reinitialize Bootstrap components after HTMX navigation
  function reinitializeBootstrapComponents() {
    // Check if bootstrap is available
    if (typeof bootstrap === 'undefined') {
      return;
    }

    // Fix any collapse elements stuck in 'collapsing' transitional state
    document.querySelectorAll('.collapsing').forEach(function(el) {
      el.classList.remove('collapsing');
      // Determine intended state from aria-expanded of its toggle
      const toggle = document.querySelector(`[href="#${el.id}"], [data-bs-target="#${el.id}"]`);
      if (toggle && toggle.getAttribute('aria-expanded') === 'true') {
        el.classList.add('collapse', 'show');
      } else {
        el.classList.add('collapse');
        el.classList.remove('show');
      }
    });

    // Remove any stuck modal-backdrop that might be blocking clicks
    document.querySelectorAll('.modal-backdrop').forEach(function(backdrop) {
      backdrop.remove();
    });
    
    // Remove modal-open class from body if no modals are visible
    const visibleModals = document.querySelectorAll('.modal.show');
    if (visibleModals.length === 0) {
      document.body.classList.remove('modal-open');
      document.body.style.overflow = '';
      document.body.style.paddingRight = '';
    }
    
    // Remove any element blocking clicks
    const clickBlockers = document.querySelectorAll('.modal-backdrop, .offcanvas-backdrop, [style*="pointer-events: none"]');
    if (clickBlockers.length > 0) {
      clickBlockers.forEach(b => b.remove());
    }
    
    // For dropdowns - DISPOSE and recreate to fix broken instances
    const dropdowns = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    dropdowns.forEach(function(dropdownToggle) {
      try {
        const existingInstance = bootstrap.Dropdown.getInstance(dropdownToggle);
        if (existingInstance) {
          existingInstance.dispose();
        }
        new bootstrap.Dropdown(dropdownToggle);
      } catch (e) {}
    });
    
    // Reinitialize all tooltips
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function(tooltipTrigger) {
      try {
        const existingInstance = bootstrap.Tooltip.getInstance(tooltipTrigger);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Tooltip(tooltipTrigger);
      } catch (e) {}
    });
    
    // Reinitialize all popovers
    document.querySelectorAll('[data-bs-toggle="popover"]').forEach(function(popoverTrigger) {
      try {
        const existingInstance = bootstrap.Popover.getInstance(popoverTrigger);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Popover(popoverTrigger);
      } catch (e) {}
    });
    
    // Reinitialize all tabs
    document.querySelectorAll('[data-bs-toggle="tab"]').forEach(function(tabTrigger) {
      try {
        const existingInstance = bootstrap.Tab.getInstance(tabTrigger);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Tab(tabTrigger);
      } catch (e) {}
    });
    
    // For collapses - DISPOSE and recreate
    const collapseToggles = document.querySelectorAll('[data-bs-toggle="collapse"]');
    collapseToggles.forEach(function(collapseTrigger) {
      const targetSelector = collapseTrigger.getAttribute('href') || collapseTrigger.getAttribute('data-bs-target');
      if (!targetSelector) return;
      
      const collapseElement = document.querySelector(targetSelector);
      if (!collapseElement) return;
      
      try {
        const existingInstance = bootstrap.Collapse.getInstance(collapseElement);
        if (existingInstance) {
          existingInstance.dispose();
        }
        new bootstrap.Collapse(collapseElement, { toggle: false });
      } catch (e) {}
    });
    
    // Also handle offcanvas (mobile sidebar)
    document.querySelectorAll('.offcanvas').forEach(function(offcanvasElement) {
      try {
        const existingInstance = bootstrap.Offcanvas.getInstance(offcanvasElement);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Offcanvas(offcanvasElement);
      } catch (e) {}
    });
  }
  
  // Also reinitialize on page load for safety
  document.addEventListener("DOMContentLoaded", reinitializeBootstrapComponents);

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
      }
    }
    scrollContentToTop(target);
    
    // Reinitialize after history restore - Bootstrap's data-api may be broken
    
    // Reinitialize Bootstrap components
    setTimeout(() => {
      reinitializeBootstrapComponents();
    }, 100);
  });

  // Handle browser back-forward cache (bfcache) - reinit when page is restored from cache
  window.addEventListener("pageshow", (event) => {
    if (event.persisted) {
      // Page was restored from bfcache
      reinitializeBootstrapComponents();
    }
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
      // Reinitialize Bootstrap components after back/forward navigation
      // Use setTimeout to ensure DOM is fully ready after bfcache restore
      setTimeout(() => {
        reinitializeBootstrapComponents();
      }, 50);
    }
  });
  window.addEventListener("popstate", () => {
    suppressOverlay(750);
    // Reinitialize Bootstrap components after popstate (browser back/forward)
    setTimeout(() => {
      reinitializeBootstrapComponents();
    }, 50);
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
    const headers = eventDetail?.headers || eventDetail?.requestConfig?.headers || eventDetail?.xhrConfig?.headers;
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

  [
    "htmx:afterSwap",
    "htmx:afterOnLoad",
    "htmx:responseError",
    "htmx:sendError",
    "htmx:timeout",
    "htmx:abort",
    "htmx:swapError",
  ].forEach((evtName) => {
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
    },
    true
  );
})();

// Handle logout to notify SignalR before disconnecting
(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    const logoutForm = document.getElementById("logout-form");

    if (logoutForm) {
      logoutForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        console.log("Logout form submitted - notifying SignalR...");

        // Try to notify SignalR of logout
        if (window.dashboardConnection && window.dashboardConnection.state === 0) {
          // 0 = Connected state
          try {
            await window.dashboardConnection.invoke("NotifyLogout");
            console.log("✓ SignalR notified of logout");
          } catch (err) {
            console.error("Failed to notify SignalR:", err);
          }

          // Give it a moment to broadcast
          await new Promise((resolve) => setTimeout(resolve, 200));
        }

        // Now submit the form
        console.log("Proceeding with logout...");
        logoutForm.submit();
      });
    }
  });
})();

// Global function for Send for Approval with SweetAlert2
function confirmSendApproval(prId, prNumber) {
  Swal.fire({
    title: '<i class="bi bi-send-fill text-primary"></i> Send for Approval?',
    html: `
      <div class="text-start">
        <p class="mb-2">PR <strong>${prNumber}</strong> akan dikirim untuk approval.</p>
        <div class="alert alert-info py-2 mb-0">
          <i class="bi bi-qr-code me-2"></i>
          <small>QR Code akan di-generate untuk proses approval</small>
        </div>
      </div>
    `,
    icon: 'question',
    showCancelButton: true,
    confirmButtonColor: '#0d6efd',
    cancelButtonColor: '#6c757d',
    confirmButtonText: '<i class="bi bi-send-fill me-1"></i> Ya, Kirim',
    cancelButtonText: '<i class="bi bi-x-lg me-1"></i> Batal',
    reverseButtons: true,
    focusCancel: true
  }).then((result) => {
    if (result.isConfirmed) {
      // Show loading
      Swal.fire({
        title: 'Mengirim...',
        html: 'Sedang memproses dan generate QR Code',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.showLoading();
        }
      });
      const form = document.getElementById('sendApprovalForm-' + prId);
      if (form) {
        form.submit();
      }
    }
  });
}
