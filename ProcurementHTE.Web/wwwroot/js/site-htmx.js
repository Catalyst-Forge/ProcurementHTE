(function () {
  "use strict";

  window.SiteApp.syncFromPartialRoot = function(root, target) {
    if (!root) return;

    const activePage = root.getAttribute("data-partial-active-page");
    const controller = root.getAttribute("data-partial-controller") || "";
    const action = root.getAttribute("data-partial-action") || "";
    const documentTitle = root.getAttribute("data-partial-document-title");

    if (activePage !== null) {
      document.body.setAttribute("data-active-page", activePage);
    }
    document.body.setAttribute("data-active-controller", controller);
    document.body.setAttribute("data-active-action", action);
    if (documentTitle) document.title = documentTitle;

    if (window.SiteApp.applyRouteState) {
        window.SiteApp.applyRouteState(controller, action);
    }
    if (window.SiteApp.scrollContentToTop) {
        window.SiteApp.scrollContentToTop(target);
    }
  };

  document.body.addEventListener("htmx:afterSwap", (event) => {
    const target = event.detail?.target;
    if (!target) return;
    
    const shouldSelfExecuteScripts = window.htmx && window.htmx.config && window.htmx.config.allowScriptTags === false;
    if (shouldSelfExecuteScripts && window.SiteApp.executeScripts) {
      window.SiteApp.executeScripts(target);
    }
    const root = target.querySelector("[data-partial-root]");
    if (root) {
      window.SiteApp.syncFromPartialRoot(root, target);
    } else {
      if (window.SiteApp.scrollContentToTop) window.SiteApp.scrollContentToTop(target);
      const bodyController = (document.body.dataset.activeController || "").toLowerCase();
      const bodyAction = (document.body.dataset.activeAction || "").toLowerCase();
      if (window.SiteApp.updateHxBoost) window.SiteApp.updateHxBoost(bodyController, bodyAction);
    }
    
    window.SiteApp.reinitializeBootstrapComponents();
  });

  window.SiteApp.reinitializeBootstrapComponents = function() {
    if (typeof bootstrap === 'undefined') return;

    document.querySelectorAll('.collapsing').forEach(function(el) {
      el.classList.remove('collapsing');
      const toggle = document.querySelector(`[href="#${el.id}"], [data-bs-target="#${el.id}"]`);
      if (toggle && toggle.getAttribute('aria-expanded') === 'true') {
        el.classList.add('collapse', 'show');
      } else {
        el.classList.add('collapse');
        el.classList.remove('show');
      }
    });

    document.querySelectorAll('.modal-backdrop').forEach(function(backdrop) {
      backdrop.remove();
    });
    
    const visibleModals = document.querySelectorAll('.modal.show');
    if (visibleModals.length === 0) {
      document.body.classList.remove('modal-open');
      document.body.style.overflow = '';
      document.body.style.paddingRight = '';
    }
    
    const clickBlockers = document.querySelectorAll('.modal-backdrop, .offcanvas-backdrop, [style*="pointer-events: none"]');
    if (clickBlockers.length > 0) {
      clickBlockers.forEach(b => b.remove());
    }
    
    const dropdowns = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    dropdowns.forEach(function(dropdownToggle) {
      try {
        const existingInstance = bootstrap.Dropdown.getInstance(dropdownToggle);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Dropdown(dropdownToggle);
      } catch (e) {}
    });
    
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function(tooltipTrigger) {
      try {
        const existingInstance = bootstrap.Tooltip.getInstance(tooltipTrigger);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Tooltip(tooltipTrigger);
      } catch (e) {}
    });
    
    document.querySelectorAll('[data-bs-toggle="popover"]').forEach(function(popoverTrigger) {
      try {
        const existingInstance = bootstrap.Popover.getInstance(popoverTrigger);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Popover(popoverTrigger);
      } catch (e) {}
    });
    
    document.querySelectorAll('[data-bs-toggle="tab"]').forEach(function(tabTrigger) {
      try {
        const existingInstance = bootstrap.Tab.getInstance(tabTrigger);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Tab(tabTrigger);
      } catch (e) {}
    });
    
    const collapseToggles = document.querySelectorAll('[data-bs-toggle="collapse"]');
    collapseToggles.forEach(function(collapseTrigger) {
      const targetSelector = collapseTrigger.getAttribute('href') || collapseTrigger.getAttribute('data-bs-target');
      if (!targetSelector) return;
      const collapseElement = document.querySelector(targetSelector);
      if (!collapseElement) return;
      try {
        const existingInstance = bootstrap.Collapse.getInstance(collapseElement);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Collapse(collapseElement, { toggle: false });
      } catch (e) {}
    });
    
    document.querySelectorAll('.offcanvas').forEach(function(offcanvasElement) {
      try {
        const existingInstance = bootstrap.Offcanvas.getInstance(offcanvasElement);
        if (existingInstance) existingInstance.dispose();
        new bootstrap.Offcanvas(offcanvasElement);
      } catch (e) {}
    });
  };
  
  document.addEventListener("DOMContentLoaded", function() {
    if(window.SiteApp.reinitializeBootstrapComponents) window.SiteApp.reinitializeBootstrapComponents();
  });

  document.body.addEventListener("htmx:beforeSwap", (event) => {
    const status = event.detail?.xhr?.status;
    if (status === 401) window.location.reload();
  });

  document.body.addEventListener("htmx:historyRestore", (event) => {
    const fragment = event.detail?.item;
    const target = document.querySelector("#app-content");
    if (fragment && typeof fragment.querySelector === "function") {
      const root = fragment.querySelector("[data-partial-root]");
      if (root) window.SiteApp.syncFromPartialRoot(root, target);
    }
    if (window.SiteApp.scrollContentToTop) window.SiteApp.scrollContentToTop(target);
    setTimeout(() => {
      if(window.SiteApp.reinitializeBootstrapComponents) window.SiteApp.reinitializeBootstrapComponents();
    }, 100);
  });
})();
