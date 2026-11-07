(function () {
  "use strict";

  const SKELETON_DELAY_MS = 300;

  let overlayFailsafe = null;
  let skeletonTimer = null;
  let readsInFlight = 0;
  let overlaySuppressUntil = 0;

  const getOverlay = () => document.getElementById("global-loading-overlay");

  const hideOverlayNow = () => {
    const loadingOverlay = getOverlay();
    if (!loadingOverlay) return;
    loadingOverlay.classList.add("d-none");
    loadingOverlay.dataset.visible = "false";
  };

  window.SiteApp.suppressOverlay = function(ms = 0) {
    overlaySuppressUntil = performance.now() + ms;
    window.SiteApp.hideGlobalLoadingOverlay();
  };

  window.SiteApp.isOverlaySuppressed = function() {
    return performance.now() < overlaySuppressUntil;
  };

  // Blocks clicks at once so nothing is sent twice. The stylesheet keeps it invisible
  // for the first 300ms, so a quick action does not flash.
  window.SiteApp.showGlobalLoadingOverlay = function() {
    const loadingOverlay = getOverlay();
    if (!loadingOverlay || loadingOverlay.dataset.visible === "true") return;
    overlaySuppressUntil = 0;
    loadingOverlay.dataset.visible = "true";
    loadingOverlay.classList.remove("d-none");
    clearTimeout(overlayFailsafe);
    overlayFailsafe = setTimeout(() => {
      window.SiteApp.hideGlobalLoadingOverlay();
    }, 15000);
  };

  window.SiteApp.hideGlobalLoadingOverlay = function() {
    clearTimeout(overlayFailsafe);
    overlayFailsafe = null;
    hideOverlayNow();
  };

  const clearSkeleton = () => {
    clearTimeout(skeletonTimer);
    document.getElementById("content-loading")?.classList.remove("is-active");
    const content = document.getElementById("app-content");
    if (!content) return;
    content.classList.remove("is-loading");
    content.removeAttribute("aria-busy");
    content.querySelector(".table-refreshing")?.classList.remove("table-refreshing");
  };

  const applySkeleton = (path) => {
    const content = document.getElementById("app-content");
    if (!content) return;
    content.setAttribute("aria-busy", "true");

    // Sorting, filtering, and paging stay on the page, so only the table has to wait.
    const samePage = new URL(path, window.location.href).pathname === window.location.pathname;
    const table = samePage ? content.querySelector(".table-responsive") : null;
    if (table) {
      table.classList.add("table-refreshing");
      return;
    }

    content.classList.add("is-loading");
    document.getElementById("content-loading")?.classList.add("is-active");
  };

  // A read only shows a skeleton once the wait is long enough to notice.
  window.SiteApp.showContentSkeleton = function(path) {
    readsInFlight += 1;
    clearTimeout(skeletonTimer);
    skeletonTimer = setTimeout(() => applySkeleton(path), SKELETON_DELAY_MS);
  };

  window.SiteApp.hideContentSkeleton = function() {
    readsInFlight = Math.max(0, readsInFlight - 1);
    if (readsInFlight === 0) clearSkeleton();
  };

  window.AppLoadingOverlay = {
    show: window.SiteApp.showGlobalLoadingOverlay,
    hide: window.SiteApp.hideGlobalLoadingOverlay,
  };

  const isBackForwardNavigation = () => {
    const nav = performance.getEntriesByType("navigation");
    const last = nav && nav.length ? nav[nav.length - 1] : null;
    return last && last.type === "back_forward";
  };

  window.addEventListener("pageshow", (event) => {
    if (event.persisted || isBackForwardNavigation()) {
      window.SiteApp.suppressOverlay(750);
      setTimeout(() => {
        if (window.SiteApp.reinitializeBootstrapComponents) {
          window.SiteApp.reinitializeBootstrapComponents();
        }
      }, 50);
    }
  });

  window.addEventListener("popstate", () => {
    window.SiteApp.suppressOverlay(750);
    setTimeout(() => {
      if (window.SiteApp.reinitializeBootstrapComponents) {
        window.SiteApp.reinitializeBootstrapComponents();
      }
    }, 50);
  });

  window.addEventListener("beforeunload", () => {
    const loadingOverlay = getOverlay();
    if (loadingOverlay?.dataset?.visible !== "true") {
      window.SiteApp.suppressOverlay(0);
    }
  });

  // htmx snapshots the page here, and Back would restore a skeleton that never ends.
  document.body.addEventListener("htmx:beforeHistorySave", () => {
    hideOverlayNow();
    clearSkeleton();
  });

  document.body.addEventListener("htmx:historyRestore", () => {
    window.SiteApp.suppressOverlay(750);
  });
})();
