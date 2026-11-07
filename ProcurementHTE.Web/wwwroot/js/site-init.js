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

  // Canvas charts cannot read var(), so they ask for the token's value here.
  window.dxColor = (name) =>
    getComputedStyle(document.documentElement).getPropertyValue(`--dx-${name}`).trim();
})();
