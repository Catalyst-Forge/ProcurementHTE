(function () {
  "use strict";
  window.notificationSystem = window.notificationSystem || {};
  const ns = window.notificationSystem;

  ns.updateBadgeCount = function(count) {
    ns.state.unreadCount = count;
    const badge = document.querySelector(".notification-badge");
    if (badge) {
      badge.textContent = count > 99 ? "99+" : count;
      badge.style.display = count > 0 ? "inline-flex" : "none";
    }
  };

  ns.updateApprovalBadge = function(count) {
    ns.state.pendingApprovalCount = count;
    const badge = document.querySelector(".pending-approval-badge");
    if (badge) {
      badge.textContent = count > 99 ? "99+" : count;
      badge.style.display = count > 0 ? "inline-flex" : "none";
    }
  };

  ns.getNotificationIcon = function(type) {
    const icons = {
      ProcurementPublished: '<i class="bi bi-megaphone text-primary fs-5"></i>',
      ApprovedByAnalyst: '<i class="bi bi-check-circle text-success fs-5"></i>',
      ApprovedByAssistantManager: '<i class="bi bi-check2-circle text-success fs-5"></i>',
      ApprovedByManager: '<i class="bi bi-check2-all text-success fs-5"></i>',
      PrRejected: '<i class="bi bi-x-circle text-danger fs-5"></i>',
      PrCompleted: '<i class="bi bi-trophy text-warning fs-5"></i>',
      ApprovalRequest: '<i class="bi bi-hourglass-split text-warning fs-5"></i>',
      ReadyForISPA: '<i class="bi bi-file-earmark-text text-info fs-5"></i>',
    };
    return icons[type] || '<i class="bi bi-bell text-primary fs-5"></i>';
  };

  ns.getTimeAgo = function(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diff = Math.floor((now - date) / 1000);

    if (diff < 60) return "Baru saja";
    if (diff < 3600) return `${Math.floor(diff / 60)} menit lalu`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} jam lalu`;
    if (diff < 604800) return `${Math.floor(diff / 86400)} hari lalu`;
    return date.toLocaleDateString("id-ID");
  };

  ns.updateNotificationList = function() {
    const listContainer = document.querySelector(".notification-list");
    if (!listContainer) return;

    if (ns.state.notifications.length === 0) {
      listContainer.innerHTML = `
        <div class="text-center text-muted py-4">
          <i class="bi bi-bell-slash fs-2"></i>
          <p class="mb-0 mt-2">Tidak ada notifikasi</p>
        </div>
      `;
      return;
    }

    listContainer.innerHTML = ns.state.notifications
      .slice(0, 5)
      .map(
        (n) => `
        <a href="${n.actionUrl || "#"}" 
           class="notification-item dropdown-item d-flex align-items-start gap-3 py-2 px-3 ${n.isRead ? "" : "unread"}"
           data-notification-id="${n.notificationId}"
           onclick="window.notificationSystem.markAsRead('${n.notificationId}')">
          <div class="notification-icon flex-shrink-0">
            ${ns.getNotificationIcon(n.notificationType)}
          </div>
          <div class="notification-content flex-grow-1 overflow-hidden">
            <div class="notification-title fw-semibold text-truncate">${n.title}</div>
            <div class="notification-message small text-muted text-truncate">${n.message}</div>
            <div class="notification-time small text-muted mt-1">
              <i class="bi bi-clock"></i> ${ns.getTimeAgo(n.createdAt)}
            </div>
          </div>
          ${!n.isRead ? '<span class="notification-dot"></span>' : ""}
        </a>
      `
      )
      .join("");
  };

})();
