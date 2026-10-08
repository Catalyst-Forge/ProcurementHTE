(function () {
  "use strict";

  var toggle = document.querySelector("[data-password-toggle]");
  if (toggle) {
    var password = document.getElementById(toggle.getAttribute("aria-controls"));
    var icon = toggle.querySelector(".bi");

    toggle.addEventListener("click", function () {
      var reveal = password.type === "password";
      password.type = reveal ? "text" : "password";
      password.toggleAttribute("data-revealed", reveal);
      toggle.setAttribute("aria-pressed", String(reveal));
      toggle.setAttribute("aria-label", reveal ? "Sembunyikan password" : "Tampilkan password");
      icon.className = reveal ? "bi bi-eye-slash" : "bi bi-eye";
      password.focus();
    });
  }

  var refresh = document.querySelector("[data-captcha-refresh]");
  var question = document.querySelector("[data-captcha-question]");
  var answer = document.getElementById("CaptchaAnswer");

  if (refresh && question) {
    refresh.addEventListener("click", function () {
      refresh.disabled = true;
      refresh.classList.add("is-spinning");
      question.classList.add("is-loading");

      fetch(refresh.dataset.url, {
        credentials: "same-origin",
        headers: { Accept: "application/json" },
      })
        .then(function (response) {
          if (!response.ok) throw new Error("captcha " + response.status);
          return response.json();
        })
        .then(function (data) {
          question.textContent = data.question + " = ?";
          if (answer) {
            answer.value = "";
            answer.focus();
          }
        })
        .catch(function () {
          question.textContent = "Gagal memuat, coba lagi";
        })
        .finally(function () {
          refresh.disabled = false;
          refresh.classList.remove("is-spinning");
          question.classList.remove("is-loading");
        });
    });
  }

  if (answer) {
    answer.addEventListener("input", function () {
      answer.value = answer.value.replace(/[^0-9]/g, "");
    });
  }

  var form = document.querySelector("[data-login-form]");
  var submit = document.querySelector("[data-login-submit]");
  if (form && submit) {
    form.addEventListener("submit", function () {
      submit.disabled = true;
      submit.classList.add("is-busy");
      submit.setAttribute("aria-busy", "true");
    });

    // Restores the button when the page comes back from the bfcache.
    window.addEventListener("pageshow", function () {
      submit.disabled = false;
      submit.classList.remove("is-busy");
      submit.removeAttribute("aria-busy");
    });
  }

  // SMIL motion ignores CSS media queries, so pause it explicitly.
  var loop = document.querySelector(".loop-svg");
  var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  function syncMotion() {
    if (!loop || typeof loop.pauseAnimations !== "function") return;
    if (reduceMotion.matches) loop.pauseAnimations();
    else loop.unpauseAnimations();
  }
  syncMotion();
  if (reduceMotion.addEventListener) reduceMotion.addEventListener("change", syncMotion);
})();
