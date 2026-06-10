(function () {
  "use strict";
  const dropdownStates = new WeakMap();
  const collapseStates = new WeakMap();

  function disableBootstrapDataApi() {
    document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(el => {
      if (!el.hasAttribute('data-manual-handled')) {
        el.setAttribute('data-manual-handled', 'dropdown');
        dropdownStates.set(el, false);
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
    
    document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(el => {
      if (!el.hasAttribute('data-manual-handled')) {
        el.setAttribute('data-manual-handled', 'collapse');
        const targetSelector = el.getAttribute('href') || el.getAttribute('data-bs-target');
        if (targetSelector) {
          const collapseElement = document.querySelector(targetSelector);
          if (collapseElement) {
            collapseElement.classList.remove('collapsing');
            const isShown = collapseElement.classList.contains('show');
            collapseStates.set(collapseElement, isShown);
          }
        }
      }
    });
  }

  disableBootstrapDataApi();
  document.body.addEventListener('htmx:afterSwap', disableBootstrapDataApi);
  document.body.addEventListener('htmx:afterSettle', disableBootstrapDataApi);

  document.addEventListener('click', function(e) {
    const dropdownToggle = e.target.closest('[data-bs-toggle="dropdown"]');
    if (dropdownToggle) {
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
      
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
      
      let wasShown = dropdownStates.get(dropdownToggle);
      if (wasShown === undefined) {
        wasShown = false;
        dropdownStates.set(dropdownToggle, false);
      }
      
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
    
    const collapseToggle = e.target.closest('[data-bs-toggle="collapse"]');
    if (collapseToggle) {
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
      
      const targetSelector = collapseToggle.getAttribute('href') || collapseToggle.getAttribute('data-bs-target');
      if (!targetSelector) return false;
      
      const collapseElement = document.querySelector(targetSelector);
      if (!collapseElement) return false;
      
      collapseElement.classList.remove('collapsing');
      
      let wasShown = collapseStates.get(collapseElement);
      if (wasShown === undefined) {
        wasShown = collapseElement.classList.contains('show');
        collapseStates.set(collapseElement, wasShown);
      }
      
      const chevron = collapseToggle.querySelector('i.bi-chevron-down, i.bi-chevron-up, .bi.bi-chevron-down, .bi.bi-chevron-up');
      
      if (!wasShown) {
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
  }, true);
  
  document.addEventListener('click', function(e) {
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
})();
