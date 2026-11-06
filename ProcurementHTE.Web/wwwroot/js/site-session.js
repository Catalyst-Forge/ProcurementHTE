(function () {
  "use strict";

  // fetch follows the login redirect silently, so an expired session would hand callers the
  // login page as if it were their response. Stop here and send the user to login instead.
  const nativeFetch = window.fetch.bind(window);
  let sessionExpired = false;

  const isLoginRedirect = (response) =>
    response.redirected &&
    new URL(response.url).pathname.toLowerCase() === "/auth/login";

  async function announceExpiredSession() {
    if (sessionExpired) return;
    sessionExpired = true;

    window.SiteApp?.hideGlobalLoadingOverlay?.(true);
    await window.Swal?.fire({
      icon: "warning",
      title: "Sesi berakhir",
      text: "Silakan login kembali.",
      allowOutsideClick: false,
    });
    window.location.reload();
  }

  window.fetch = async (...args) => {
    const response = await nativeFetch(...args);
    if (!isLoginRedirect(response)) return response;

    announceExpiredSession();
    return new Promise(() => {});
  };
})();
