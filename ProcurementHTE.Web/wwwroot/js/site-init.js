(function () {
  "use strict";
  if (window.__siteJsInitialized) {
    console.log('[site.js] Already initialized, skipping');
    return;
  }
  window.__siteJsInitialized = true;
  if ("scrollRestoration" in history) {
    history.scrollRestoration = "manual";
  }
  window.SiteApp = {};
})();
