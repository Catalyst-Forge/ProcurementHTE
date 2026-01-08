// Notification System for ProcurementHTE
// This script handles real-time notifications via SignalR and notification UI management

(function () {
  "use strict";

  // Check if SignalR is available
  if (typeof signalR === "undefined") {
    console.warn("SignalR library not loaded - notifications will not work in real-time");
    return;
  }

  // Notification state
  let notificationConnection = null;
  let isConnected = false;
  let unreadCount = 0;
  let notifications = [];

  // Initialize SignalR connection for notifications
  function initNotificationConnection() {
    notificationConnection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/dashboard")
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Listen for new notifications
    notificationConnection.on("ReceiveNotification", function (data) {
      console.log("📬 New notification received:", data);
      handleNewNotification(data);
    });

    // Listen for badge count updates
    notificationConnection.on("UpdateNotificationBadge", function (data) {
      console.log("🔢 Badge count updated:", data.unreadCount);
      updateBadgeCount(data.unreadCount);
    });

    // Start connection
    notificationConnection
      .start()
      .then(() => {
        console.log("✅ Notification SignalR connected");
        isConnected = true;
        loadNotifications();
      })
      .catch((error) => {
        console.error("❌ Failed to connect to notification hub:", error);
        // Retry connection after 5 seconds
        setTimeout(initNotificationConnection, 5000);
      });

    // Handle reconnection
    notificationConnection.onreconnected(() => {
      console.log("🔄 Notification SignalR reconnected");
      isConnected = true;
      loadNotifications();
    });

    notificationConnection.onclose(() => {
      console.log("❌ Notification SignalR disconnected");
      isConnected = false;
    });
  }

  // Handle incoming notification
  function handleNewNotification(data) {
    // Add to notifications array
    notifications.unshift({
      notificationId: data.notificationId,
      title: data.title,
      message: data.message,
      notificationType: data.notificationType,
      actionUrl: data.actionUrl,
      createdAt: data.createdAt,
      isRead: false,
    });

    // Update badge count
    unreadCount = data.unreadCount || unreadCount + 1;
    updateBadgeCount(unreadCount);

    // Update notification list UI
    updateNotificationList();

    // Show toast notification
    showToastNotification(data);

    // Play notification sound (optional)
    playNotificationSound();

    // Request browser notification permission and show
    showBrowserNotification(data);
  }

  // Update the badge count display
  function updateBadgeCount(count) {
    unreadCount = count;
    const badge = document.querySelector(".notification-badge");
    if (badge) {
      badge.textContent = count > 99 ? "99+" : count;
      badge.style.display = count > 0 ? "inline-flex" : "none";
    }
  }

  // Load notifications from API
  async function loadNotifications(loadAll = false) {
    try {
      const take = loadAll ? 100 : 5;
      const response = await fetch(`/api/notifications?take=${take}`);
      if (!response.ok) throw new Error("Failed to load notifications");

      const data = await response.json();
      notifications = data.notifications || [];
      unreadCount = data.unreadCount || 0;

      updateBadgeCount(unreadCount);
      updateNotificationList();
    } catch (error) {
      console.error("Failed to load notifications:", error);
    }
  }

  // Update notification list in dropdown
  function updateNotificationList() {
    const listContainer = document.querySelector(".notification-list");
    if (!listContainer) return;

    if (notifications.length === 0) {
      listContainer.innerHTML = `
        <div class="text-center text-muted py-4">
          <i class="bi bi-bell-slash fs-2"></i>
          <p class="mb-0 mt-2">Tidak ada notifikasi</p>
        </div>
      `;
      return;
    }

    // Only show 5 most recent notifications in dropdown
    listContainer.innerHTML = notifications
      .slice(0, 5)
      .map(
        (n) => `
        <a href="${n.actionUrl || "#"}" 
           class="notification-item dropdown-item d-flex align-items-start gap-3 py-2 px-3 ${n.isRead ? "" : "unread"}"
           data-notification-id="${n.notificationId}"
           onclick="window.notificationSystem.markAsRead('${n.notificationId}')">
          <div class="notification-icon flex-shrink-0">
            ${getNotificationIcon(n.notificationType)}
          </div>
          <div class="notification-content flex-grow-1 overflow-hidden">
            <div class="notification-title fw-semibold text-truncate">${n.title}</div>
            <div class="notification-message small text-muted text-truncate">${n.message}</div>
            <div class="notification-time small text-muted mt-1">
              <i class="bi bi-clock"></i> ${getTimeAgo(n.createdAt)}
            </div>
          </div>
          ${!n.isRead ? '<span class="notification-dot"></span>' : ""}
        </a>
      `
      )
      .join("");
  }

  // Get icon based on notification type
  function getNotificationIcon(type) {
    const icons = {
      ProcurementPublished: '<i class="bi bi-megaphone text-primary fs-5"></i>',
      ApprovedByAnalyst: '<i class="bi bi-check-circle text-success fs-5"></i>',
      ApprovedByAssistantManager: '<i class="bi bi-check2-circle text-success fs-5"></i>',
      ApprovedByManager: '<i class="bi bi-check2-all text-success fs-5"></i>',
      PrRejected: '<i class="bi bi-x-circle text-danger fs-5"></i>',
      PrCompleted: '<i class="bi bi-trophy text-warning fs-5"></i>',
    };
    return icons[type] || '<i class="bi bi-bell text-primary fs-5"></i>';
  }

  // Calculate time ago
  function getTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diff = Math.floor((now - date) / 1000);

    if (diff < 60) return "Baru saja";
    if (diff < 3600) return `${Math.floor(diff / 60)} menit lalu`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} jam lalu`;
    if (diff < 604800) return `${Math.floor(diff / 86400)} hari lalu`;
    return date.toLocaleDateString("id-ID");
  }

  // Show toast notification using SweetAlert2
  function showToastNotification(data) {
    // Check if SweetAlert2 is available
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

    // Fallback to Bootstrap toast if SweetAlert2 not available
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
  }

  // Play notification sound
  function playNotificationSound() {
    try {
      const audio = new Audio("/sounds/notification.mp3");
      audio.volume = 0.5;
      audio.play().catch(() => {
        // Ignore errors - browser might block autoplay
      });
    } catch (e) {
      // Ignore errors
    }
  }

  // Show browser notification
  function showBrowserNotification(data) {
    if (!("Notification" in window)) return;

    if (Notification.permission === "granted") {
      const notification = new Notification(data.title, {
        body: data.message,
        icon: "/favicon.ico",
        tag: data.notificationId,
      });

      notification.onclick = function () {
        window.focus();
        if (data.actionUrl) {
          window.location.href = data.actionUrl;
        }
        notification.close();
      };

      setTimeout(() => notification.close(), 5000);
    } else if (Notification.permission !== "denied") {
      Notification.requestPermission();
    }
  }

  // Mark notification as read
  async function markAsRead(notificationId) {
    try {
      const response = await fetch(`/api/notifications/${notificationId}/read`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });

      if (response.ok) {
        const result = await response.json();
        updateBadgeCount(result.unreadCount);

        // Update local notification state
        const notification = notifications.find((n) => n.notificationId === notificationId);
        if (notification) notification.isRead = true;

        updateNotificationList();
      }
    } catch (error) {
      console.error("Failed to mark notification as read:", error);
    }
  }

  // Mark all notifications as read
  async function markAllAsRead() {
    try {
      const response = await fetch("/api/notifications/read-all", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });

      if (response.ok) {
        updateBadgeCount(0);
        notifications.forEach((n) => (n.isRead = true));
        updateNotificationList();
      }
    } catch (error) {
      console.error("Failed to mark all notifications as read:", error);
    }
  }

  // Initialize on DOM ready
  document.addEventListener("DOMContentLoaded", function () {
    initNotificationConnection();

    // Setup mark all as read button
    const markAllBtn = document.querySelector(".mark-all-read-btn");
    if (markAllBtn) {
      markAllBtn.addEventListener("click", function (e) {
        e.preventDefault();
        markAllAsRead();
      });
    }

    // Request browser notification permission
    if ("Notification" in window && Notification.permission === "default") {
      Notification.requestPermission();
    }
  });

  // Expose functions globally for inline onclick handlers
  window.notificationSystem = {
    markAsRead,
    markAllAsRead,
    loadNotifications,
    getUnreadCount: () => unreadCount,
  };
})();
