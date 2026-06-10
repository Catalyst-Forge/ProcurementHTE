(function () {
  "use strict";
  window.notificationSystem = window.notificationSystem || {};
  const ns = window.notificationSystem;

  ns.showToastNotification = function(data) {
    if (typeof Swal !== "undefined" && typeof Swal.fire === "function") {
      const Toast = Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: true,
        confirmButtonText: "Lihat",
        showCloseButton: true,
        timer: 8000,
        timerProgressBar: true,
        didOpen: (toast) => {
          toast.addEventListener("mouseenter", Swal.stopTimer);
          toast.addEventListener("mouseleave", Swal.resumeTimer);
        },
      });

      Toast.fire({
        icon: "info",
        title: data.title,
        text: data.message,
      }).then((result) => {
        if (result.isConfirmed && data.actionUrl) {
          window.location.href = data.actionUrl;
        }
      });
      return;
    }

    const toastContainer = document.querySelector(".toast-container");
    if (!toastContainer) return;

    const toastId = `toast-${Date.now()}`;
    const toastHtml = `
      <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="toast-header bg-primary text-white">
          <i class="bi bi-bell me-2"></i>
          <strong class="me-auto">${data.title}</strong>
          <small>Baru saja</small>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body">
          ${data.message}
          ${
            data.actionUrl
              ? `<div class="mt-2"><a href="${data.actionUrl}" class="btn btn-sm btn-primary">Lihat Detail</a></div>`
              : ""
          }
        </div>
      </div>
    `;

    toastContainer.insertAdjacentHTML("beforeend", toastHtml);
    const toastEl = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastEl, { delay: 8000 });
    toast.show();
    toastEl.addEventListener("hidden.bs.toast", () => toastEl.remove());
  };

  ns.playNotificationSound = function() {
    try {
      const audio = new Audio("/sounds/notification.mp3");
      audio.volume = 0.5;
      audio.play().catch(() => {});
    } catch (e) {}
  };

  ns.showBrowserNotification = function(data) {
    if (!("Notification" in window)) return;

    if (Notification.permission === "granted") {
      const notification = new Notification(data.title, {
        body: data.message,
        icon: "/favicon.ico",
        tag: data.notificationId,
      });

      notification.onclick = function () {
        window.focus();
        if (data.actionUrl) window.location.href = data.actionUrl;
        notification.close();
      };
      setTimeout(() => notification.close(), 5000);
    } else if (Notification.permission !== "denied") {
      Notification.requestPermission();
    }
  };

  document.addEventListener("DOMContentLoaded", function () {
    if(ns.loadPendingApprovalCount) ns.loadPendingApprovalCount();
    if(ns.initNotificationConnection) ns.initNotificationConnection();

    const markAllBtn = document.querySelector(".mark-all-read-btn");
    if (markAllBtn) {
      markAllBtn.addEventListener("click", function (e) {
        e.preventDefault();
        ns.markAllAsRead();
      });
    }

    if ("Notification" in window && Notification.permission === "default") {
      Notification.requestPermission();
    }

    document.addEventListener("visibilitychange", function () {
      if (document.visibilityState === "visible" && ns.state && ns.state.isConnected) {
        setTimeout(function () {
          if(ns.loadPendingApprovalCount) ns.loadPendingApprovalCount();
          if(ns.loadNotifications) ns.loadNotifications();
        }, 500);
      }
    });

    window.addEventListener("focus", function () {
      if (ns.state && ns.state.isConnected && ns.loadPendingApprovalCount) {
        ns.loadPendingApprovalCount();
      }
    });
  });
})();
