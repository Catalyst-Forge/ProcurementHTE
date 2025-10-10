document.addEventListener("DOMContentLoaded", function () {
  const activePage = document.body.getAttribute("data-active-page");
  const navLinks = document.querySelectorAll(".nav-link");
  const currentPath = window.location.pathname.toLowerCase();

  navLinks.forEach(link => {
    link.classList.remove("active");

    // Skip parent menu
    if (link.hasAttribute("data-bs-toggle")) return;

    const page = link.getAttribute("data-page");
    const href = link.getAttribute("href")?.toLowerCase();

    // Check if active
    const isActive =
      (activePage && page === activePage) ||
      (href && (
        currentPath === href ||
        (href !== "/" && currentPath.startsWith(href)) ||
        (href === "/" && (currentPath === "/" || currentPath === "/home"))
      ));

    if (isActive) {
      link.classList.add("active");

      // Open parent collapse if nested
      const collapse = link.closest(".collapse");
      if (collapse) {
        bootstrap.Collapse.getOrCreateInstance(collapse, { toggle: false }).show();

        const parentToggle = document.querySelector(`[href="#${collapse.id}"]`);
        const chevron = parentToggle?.querySelector(".bi-chevron-down");
        if (chevron) chevron.classList.add("rotate");
      }
    }
  });

  // Handle chevron rotation on parent toggle
  document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(toggle => {
    const chevron = toggle.querySelector(".bi-chevron-down");
    if (!chevron) return;

    const targetId = toggle.getAttribute("href");
    const target = document.querySelector(targetId);

    // Set initial state berdasarkan collapse state
    if (target?.classList.contains("show")) {
      chevron.classList.add("rotate");
    }

    // Toggle on click
    toggle.addEventListener("click", () => chevron.classList.toggle("rotate"));

    // Sync dengan event collapse (untuk memastikan sync sempurna)
    if (target) {
      target.addEventListener("shown.bs.collapse", () => chevron.classList.add("rotate"));
      target.addEventListener("hidden.bs.collapse", () => chevron.classList.remove("rotate"));
    }
  });
});