(function () {
  "use strict";

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
    if (!collapseId) return;

    const selector = [`[href="#${collapseId}"]`, `[data-bs-target="#${collapseId}"]`].join(", ");
    document.querySelectorAll(selector).forEach((toggle) => {
      toggle.setAttribute("aria-expanded", shouldShow ? "true" : "false");
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

  window.SiteApp.updateHxBoost = function(controller, action) {
    const main = document.getElementById("app-content");
    if (!main) return;
    const heavyPnL = controller === "procurements" && (action === "createprofitloss" || action === "editprofitloss");
    main.setAttribute("hx-boost", heavyPnL ? "false" : "true");
  };

  window.SiteApp.applyRouteState = function(controller, action) {
    const normalizedController = (controller || "").toLowerCase();
    const normalizedAction = (action || "").toLowerCase();

    window.SiteApp.updateHxBoost(normalizedController, normalizedAction);

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
  };

  window.SiteApp.scrollContentToTop = function(target) {
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
  };

  window.SiteApp.executeScripts = function(scope) {
    if (!scope) return;
    scope.querySelectorAll("script").forEach((script) => {
      const type = (script.type || "").trim();
      const isModule = type === "module";
      const executable = !type || type === "text/javascript" || isModule;
      if (!executable) return;

      const newScript = document.createElement("script");
      if (type) newScript.type = type;

      script.getAttributeNames().forEach((attr) => {
        if (attr === "src" || attr === "type") return;
        const value = script.getAttribute(attr);
        if (value !== null) newScript.setAttribute(attr, value);
      });

      if (script.src) newScript.src = script.src;
      else newScript.textContent = script.textContent;

      script.replaceWith(newScript);
    });
  };
})();
