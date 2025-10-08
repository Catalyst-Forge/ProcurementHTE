// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
  const activePage = document.body.getAttribute("data-active-page");
  const navLinks = document.querySelectorAll(".nav-link");
  const currentPath = window.location.pathname.toLowerCase();

  navLinks.forEach((link) => {
    link.classList.remove("active");

    if (activePage) {
      const linkPage = link.getAttribute("data-page");
      if (linkPage === activePage) {
        link.classList.add("active");
        return;
      }
    }

    const linkHref = link.getAttribute("href").toLowerCase();
    if (linkHref) {
      if (currentPath === linkHref) {
        // link.classList.add("active");
      } else if (linkHref !== "/" && currentPath.startsWith(linkHref)) {
        link.classList.add("active");
      } else if (linkHref === "/" && (currentPath === "/" || currentPath === "/home")) {
        link.classList.add("active");
      }
    }
  });
});
