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
    executeScripts(target);
    const root = target.querySelector("[data-partial-root]");
    if (root) {
      syncFromPartialRoot(root, target);
    } else {
      scrollContentToTop(target);
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
})();
