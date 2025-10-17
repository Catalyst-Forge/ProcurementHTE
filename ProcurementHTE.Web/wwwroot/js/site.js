document.addEventListener("DOMContentLoaded", function () {
  const activePage = document.body.getAttribute("data-active-page");
  const navLinks = document.querySelectorAll(".nav-link");
  const currentPath = window.location.pathname.toLowerCase();

  navLinks.forEach((link) => {
    // Skip parent menu with toggle
    if (link.hasAttribute("data-bs-toggle")) return;

    const page = link.getAttribute("data-page");
    const href = link.getAttribute("href")?.toLowerCase();

    // Check if active
    const isActive =
      (activePage && page === activePage) ||
      (href &&
        href !== "#" &&
        (currentPath === href ||
          (href !== "/" && currentPath.startsWith(href)) ||
          (href === "/" && (currentPath === "/" || currentPath === "/home"))));

    if (isActive) {
      link.classList.add("active");

      // Open all parent collapses recursively
      let currentElement = link;
      while (currentElement) {
        const parentCollapse = currentElement.closest(".collapse");

        if (parentCollapse) {
          // Show the collapse
          const collapse = bootstrap.Collapse.getOrCreateInstance(parentCollapse, { toggle: false });
          collapse.show();

          // Rotate the chevron of the toggle button
          const toggleButton = document.querySelector(`[href="#${parentCollapse.id}"]`);
          const chevron = toggleButton?.querySelector(".bi-chevron-down");
          if (chevron) {
            chevron.classList.add("rotate");
          }

          // Move up to find next parent
          currentElement = parentCollapse.parentElement;
        } else {
          break;
        }
      }
    }
  });

  // Handle chevron rotation on click
  document.querySelectorAll('[data-bs-toggle="collapse"]').forEach((toggle) => {
    const chevron = toggle.querySelector(".bi-chevron-down");
    if (!chevron) return;

    const targetId = toggle.getAttribute("href");
    const target = document.querySelector(targetId);

    if (target) {
      target.addEventListener("shown.bs.collapse", () => chevron.classList.add("rotate"));
      target.addEventListener("hidden.bs.collapse", () => chevron.classList.remove("rotate"));
    }
  });
});
