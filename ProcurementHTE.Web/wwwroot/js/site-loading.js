(function () {
  "use strict";

  let overlayFailsafe = null;
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

  document.body.addEventListener("htmx:beforeHistorySave", () => {
    hideOverlayNow();
  });

  document.body.addEventListener("htmx:historyRestore", () => {
    window.SiteApp.suppressOverlay(750);
  });
})();
