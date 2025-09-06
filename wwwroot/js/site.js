// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function() {
  const activePage = document.body.getAttribute('data-active-page');
  const navLinks = document.querySelectorAll(".nav-link");
  
  if (activePage) {
    navLinks.forEach((link) => {
      link.classList.remove("active");
      
      // Tambahkan data-page attribute ke setiap nav-link
      const linkPage = link.getAttribute('data-page');
      if (linkPage === activePage) {
        link.classList.add("active");
      }
    });
  }
});
