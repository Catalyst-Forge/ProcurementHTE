// Real-time Dashboard Updates with SignalR
(function () {
  "use strict";

  // Check if SignalR is available
  if (typeof signalR === "undefined") {
    console.warn("SignalR library not loaded");
    return;
  }

  // Check if user is on dashboard page
  const isDashboardPage = document.querySelector("[data-dashboard-realtime]");
  if (!isDashboardPage) {
    return;
  }

  // Initialize SignalR connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/dashboard")
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Track connection state
  let isConnected = false;
  let isPageVisible = true;

  // Handle page visibility changes (tab switching, minimizing)
  document.addEventListener("visibilitychange", function () {
    isPageVisible = !document.hidden;
    if (isPageVisible && isConnected) {
      console.log("Page visible again, checking connection...");
      // Refresh data when user comes back to the tab
      refreshDashboardData();
    }
  });

  // Handle browser close/refresh - attempt to notify server
  window.addEventListener("beforeunload", function (event) {
    console.log("beforeunload triggered - notifying server of logout");
    // Try to disconnect gracefully
    if (isConnected && connection.state === signalR.HubConnectionState.Connected) {
      // Call NotifyLogout synchronously before page unloads
      try {
        connection.invoke("NotifyLogout").catch((err) => {
          console.error("Failed to notify logout:", err);
        });
      } catch (err) {
        console.error("Error invoking NotifyLogout:", err);
      }

      // Use synchronous beacon API for reliable delivery on page unload
      const disconnectUrl = "/api/user/disconnect";
      if (navigator.sendBeacon) {
        navigator.sendBeacon(disconnectUrl);
      }

      // Also try to stop connection (may not complete)
      connection.stop().catch(console.error);
    }
  });

  // Connection established
  connection.onreconnecting((error) => {
    console.warn("Dashboard connection lost, reconnecting...", error);
    isConnected = false;
    updateConnectionStatus("reconnecting");
  });

  connection.onreconnected((connectionId) => {
    console.log("Dashboard reconnected:", connectionId);
    isConnected = true;
    updateConnectionStatus("connected");
    startHeartbeat(); // Restart heartbeat after reconnection
    // Rejoin dashboard_viewers group after reconnection
    connection.invoke("JoinDashboardViewers")
      .then(() => console.log("Rejoined dashboard_viewers group"))
      .catch(err => console.error("Failed to rejoin dashboard_viewers group:", err));
    // Refresh dashboard data after reconnection
    refreshDashboardData();
  });

  connection.onclose((error) => {
    console.error("Dashboard connection closed:", error);
    stopHeartbeat(); // Stop heartbeat when disconnected
    isConnected = false;
    updateConnectionStatus("disconnected");
  });

  // Listen for user activity changes (online/offline)
  connection.on("UserActivityChanged", function (data) {
    console.log("=== USER ACTIVITY CHANGED EVENT RECEIVED ===");
    console.log("Raw data:", JSON.stringify(data, null, 2));
    console.log("UserId:", data.userId, "Type:", typeof data.userId);
    console.log("IsOnline:", data.isOnline, "Type:", typeof data.isOnline);
    console.log("UserName:", data.userName);
    console.log("FullName:", data.fullName);
    console.log("===========================================");
    updateUserActivityUI(data);
  });

  // Listen for dashboard data updates
  connection.on("DashboardDataUpdated", function () {
    console.log("Dashboard data updated");
    refreshDashboardData();
  });

  // Start connection
  connection
    .start()
    .then(() => {
      console.log("Dashboard SignalR connected");
      isConnected = true;
      updateConnectionStatus("connected");
      startHeartbeat(); // Start sending heartbeats
      
      // Join dashboard_viewers group to receive UserActivityChanged events
      connection.invoke("JoinDashboardViewers")
        .then(() => console.log("Joined dashboard_viewers group"))
        .catch(err => console.error("Failed to join dashboard_viewers group:", err));
    })
    .catch((error) => {
      console.error("Failed to connect to dashboard hub:", error);
      updateConnectionStatus("error");
      // Retry connection after 5 seconds
      setTimeout(() => {
        if (!isConnected) {
          connection.start().catch(console.error);
        }
      }, 5000);
    });

  // Update connection status indicator
  function updateConnectionStatus(status) {
    const indicator = document.getElementById("realtime-status");
    if (!indicator) return;

    indicator.className = "realtime-status";
    switch (status) {
      case "connected":
        indicator.classList.add("status-connected");
        indicator.title = "Real-time updates active";
        break;
      case "reconnecting":
        indicator.classList.add("status-reconnecting");
        indicator.title = "Reconnecting...";
        break;
      case "disconnected":
      case "error":
        indicator.classList.add("status-disconnected");
        indicator.title = "Real-time updates unavailable";
        break;
    }
  }

  // Update user activity in the UI
  function updateUserActivityUI(data) {
    console.log("=== UPDATE USER ACTIVITY UI START ===");
    console.log("Looking for user ID:", data.userId);

    // Get all user rows to debug selector
    const allUserRows = document.querySelectorAll("tr[data-user-id]");
    console.log("Total user rows found:", allUserRows.length);
    if (allUserRows.length > 0) {
      console.log(
        "Sample user IDs in table:",
        Array.from(allUserRows)
          .slice(0, 3)
          .map((r) => r.getAttribute("data-user-id"))
          .join(", ")
      );
    }

    const userRow = document.querySelector(`tr[data-user-id="${data.userId}"]`);
    if (!userRow) {
      // User not in current view, might need to refresh the whole list
      console.log("❌ User row NOT FOUND for ID:", data.userId);
      console.log(
        "All user IDs in table:",
        Array.from(allUserRows).map((r) => r.getAttribute("data-user-id"))
      );
      console.log("Refreshing user activity table...");
      refreshUserActivityTable();
      return;
    }

    console.log("✓ Found user row for ID:", data.userId);
    console.log("Current row status:", userRow.getAttribute("data-user-status"));
    console.log("New status:", data.isOnline ? "online" : "offline");

    // Update data-user-status attribute
    userRow.setAttribute("data-user-status", data.isOnline ? "online" : "offline");

    // Update status icon (first td with icon)
    const iconCell = userRow.querySelector(".user-status-icon");
    if (iconCell) {
      if (data.isOnline) {
        // Online: person-fill with pulse indicator
        iconCell.className = "bi bi-person-fill text-success fs-5 user-status-icon";
        iconCell.innerHTML = "";
        const parentDiv = iconCell.parentElement;
        if (parentDiv && !parentDiv.querySelector(".position-absolute")) {
          const pulseSpan = document.createElement("span");
          pulseSpan.className =
            "position-absolute top-0 start-100 translate-middle p-1 bg-success border border-light rounded-circle";
          pulseSpan.innerHTML = '<span class="visually-hidden">Online</span>';
          parentDiv.style.position = "relative";
          parentDiv.appendChild(pulseSpan);
        }
      } else {
        // Offline: person outline without pulse
        iconCell.className = "bi bi-person text-secondary fs-5 user-status-icon";
        const parentDiv = iconCell.parentElement;
        if (parentDiv) {
          const pulseSpan = parentDiv.querySelector(".position-absolute");
          if (pulseSpan) pulseSpan.remove();
        }
      }
    }

    // Update status badge
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

    // Update timestamp
    const timestamp = userRow.querySelector(".user-last-activity");
    if (timestamp) {
      const timeAgo = data.timestamp ? getTimeAgo(new Date(data.timestamp)) : "Baru saja";
      timestamp.className = data.isOnline ? "text-success" : "text-muted";
      timestamp.innerHTML = `<i class="bi bi-clock"></i> ${data.isOnline ? "Online" : "Offline"} - ${timeAgo}`;
    }

    // Update online users count
    updateOnlineUsersCount();

    // Log successful update (no toast notification needed)
    console.log(`✓ User ${data.userId} status updated to ${data.isOnline ? "ONLINE" : "OFFLINE"}`);
  }

  // Refresh the entire user activity table
  function refreshUserActivityTable() {
    const tableContainer = document.querySelector("[data-user-activity-table]");
    if (!tableContainer) return;

    // Show loading state
    const loadingHtml =
      '<div class="text-center py-3"><div class="spinner-border spinner-border-sm" role="status"><span class="visually-hidden">Loading...</span></div></div>';

    fetch(window.location.href, {
      headers: {
        "X-Requested-With": "XMLHttpRequest",
      },
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
      .catch((error) => {
        console.error("Failed to refresh user activity table:", error);
      });
  }

  // Update online users count badge
  function updateOnlineUsersCount() {
    const onlineBadge = document.querySelector("[data-online-users-count]");
    const offlineBadge = document.querySelector("[data-offline-users-count]");

    const allRows = document.querySelectorAll("[data-user-id]");
    const onlineRows = document.querySelectorAll('[data-user-status="online"]');
    const offlineRows = document.querySelectorAll('[data-user-status="offline"]');

    if (onlineBadge) {
      onlineBadge.textContent = onlineRows.length;
    }

    if (offlineBadge) {
      offlineBadge.textContent = offlineRows.length;
    }

    console.log(
      `Counter updated - Online: ${onlineRows.length}, Offline: ${offlineRows.length}, Total: ${allRows.length}`
    );
  }

  // Refresh entire dashboard data
  function refreshDashboardData() {
    // Trigger a soft refresh of dashboard metrics without full page reload
    console.log("Refreshing dashboard metrics...");

    // You can implement partial updates here
    // For now, we'll just refresh the user activity table
    refreshUserActivityTable();
  }

  // Helper: Calculate time ago
  function getTimeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);

    if (seconds < 60) return "Baru saja";

    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes} menit yang lalu`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} jam yang lalu`;

    const days = Math.floor(hours / 24);
    if (days < 7) return `${days} hari yang lalu`;

    return date.toLocaleDateString("id-ID");
  }

  // Heartbeat management
  let heartbeatInterval = null;

  function startHeartbeat() {
    // Clear any existing heartbeat
    if (heartbeatInterval) {
      clearInterval(heartbeatInterval);
    }

    // Send heartbeat every 2 minutes
    heartbeatInterval = setInterval(() => {
      if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("UpdateActivity").catch((err) => console.error("Failed to send heartbeat:", err));
      }
    }, 2 * 60 * 1000); // 2 minutes

    console.log("Heartbeat started");
  }

  function stopHeartbeat() {
    if (heartbeatInterval) {
      clearInterval(heartbeatInterval);
      heartbeatInterval = null;
      console.log("Heartbeat stopped");
    }
  }

  // Handle page visibility changes
  document.addEventListener("visibilitychange", () => {
    if (!document.hidden && connection.state !== signalR.HubConnectionState.Connected) {
      console.log("Page visible, checking connection...");
      connection.start().catch(console.error);
    }
  });

  // Handle beforeunload to gracefully stop connection
  window.addEventListener("beforeunload", () => {
    stopHeartbeat();
    if (connection.state === signalR.HubConnectionState.Connected) {
      connection.stop();
    }
  });

  //
  // Expose connection for debugging
  window.dashboardConnection = connection;
})();
