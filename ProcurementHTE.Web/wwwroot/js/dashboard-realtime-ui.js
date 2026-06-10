// Real-time Dashboard UI Updates
window.DashboardUI = {
  updateUserActivityUI: function(data) {
    console.log("=== UPDATE USER ACTIVITY UI START ===");
    console.log("Looking for user ID:", data.userId);

    const allUserRows = document.querySelectorAll("tr[data-user-id]");
    if (allUserRows.length > 0) {
      console.log(
        "Sample user IDs in table:",
        Array.from(allUserRows).slice(0, 3).map((r) => r.getAttribute("data-user-id")).join(", ")
      );
    }

    const userRow = document.querySelector("tr[data-user-id='" + data.userId + "']");
    if (!userRow) {
      console.log("? User row NOT FOUND for ID:", data.userId);
      this.refreshUserActivityTable();
      return;
    }

    console.log("? Found user row for ID:", data.userId);
    userRow.setAttribute("data-user-status", data.isOnline ? "online" : "offline");

    const iconCell = userRow.querySelector(".user-status-icon");
    if (iconCell) {
      if (data.isOnline) {
        iconCell.className = "bi bi-person-fill text-success fs-5 user-status-icon";
        iconCell.innerHTML = "";
        const parentDiv = iconCell.parentElement;
        if (parentDiv && !parentDiv.querySelector(".position-absolute")) {
          const pulseSpan = document.createElement("span");
          pulseSpan.className = "position-absolute top-0 start-100 translate-middle p-1 bg-success border border-light rounded-circle";
          pulseSpan.innerHTML = '<span class="visually-hidden">Online</span>';
          parentDiv.style.position = "relative";
          parentDiv.appendChild(pulseSpan);
        }
      } else {
        iconCell.className = "bi bi-person text-secondary fs-5 user-status-icon";
        const parentDiv = iconCell.parentElement;
        if (parentDiv) {
          const pulseSpan = parentDiv.querySelector(".position-absolute");
          if (pulseSpan) pulseSpan.remove();
        }
      }
    }

    const badge = userRow.querySelector(".user-status-badge");
    if (badge) {
      badge.className = "badge user-status-badge";
      if (data.isOnline) {
        badge.classList.add("bg-success");
        badge.innerHTML = '<i class="bi bi-circle-fill" style="font-size: 6px;"></i> Online';
      } else {
        badge.classList.add("bg-secondary");
        badge.innerHTML = '<i class="bi bi-circle" style="font-size: 6px;"></i> Offline';
      }
    }

    const timestamp = userRow.querySelector(".user-last-activity");
    if (timestamp) {
      const timeAgo = data.timestamp ? this.getTimeAgo(new Date(data.timestamp)) : "Baru saja";
      timestamp.className = data.isOnline ? "text-success" : "text-muted";
      timestamp.innerHTML = '<i class="bi bi-clock"></i> ' + (data.isOnline ? "Online" : "Offline") + ' - ' + timeAgo;
    }

    this.updateOnlineUsersCount();
    console.log(? User  status updated to );
  },

  refreshUserActivityTable: function() {
    const tableContainer = document.querySelector("[data-user-activity-table]");
    if (!tableContainer) return;

    fetch(window.location.href, {
      headers: { "X-Requested-With": "XMLHttpRequest" }
    })
      .then((response) => response.text())
      .then((html) => {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, "text/html");
        const newTable = doc.querySelector("[data-user-activity-table]");
        if (newTable) {
          tableContainer.innerHTML = newTable.innerHTML;
        }
      })
      .catch((error) => console.error("Failed to refresh user activity table:", error));
  },

  updateOnlineUsersCount: function() {
    const onlineBadge = document.querySelector("[data-online-users-count]");
    const offlineBadge = document.querySelector("[data-offline-users-count]");
    const allRows = document.querySelectorAll("[data-user-id]");
    const onlineRows = document.querySelectorAll('[data-user-status="online"]');
    const offlineRows = document.querySelectorAll('[data-user-status="offline"]');

    if (onlineBadge) onlineBadge.textContent = onlineRows.length;
    if (offlineBadge) offlineBadge.textContent = offlineRows.length;
  },

  refreshDashboardData: function() {
    console.log("Refreshing dashboard metrics...");
    this.refreshUserActivityTable();
  },

  getTimeAgo: function(date) {
    const seconds = Math.floor((new Date() - date) / 1000);
    if (seconds < 60) return "Baru saja";
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return minutes + " menit yang lalu";
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours + " jam yang lalu";
    const days = Math.floor(hours / 24);
    if (days < 7) return days + " hari yang lalu";
    return date.toLocaleDateString("id-ID");
  }
};
