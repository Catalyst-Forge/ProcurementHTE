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
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  // Track connection state
  let isConnected = false;
  let isPageVisible = true;

  // Handle page visibility changes (tab switching, minimizing)
  document.addEventListener("visibilitychange", function () {
    isPageVisible = !document.hidden;
    if (isPageVisible && isConnected) {
      // Refresh data when user comes back to the tab
      window.DashboardUI.refreshDashboardData();
    }
  });

  // Handle browser close/refresh - attempt to notify server
  window.addEventListener("beforeunload", function (event) {
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
    isConnected = true;
    updateConnectionStatus("connected");
    startHeartbeat(); // Restart heartbeat after reconnection
    // Rejoin dashboard_viewers group after reconnection
    connection.invoke("JoinDashboardViewers")
      .catch(err => console.error("Failed to rejoin dashboard_viewers group:", err));
    // Refresh dashboard data after reconnection
    window.DashboardUI.refreshDashboardData();
  });

  connection.onclose((error) => {
    console.error("Dashboard connection closed:", error);
    stopHeartbeat(); // Stop heartbeat when disconnected
    isConnected = false;
    updateConnectionStatus("disconnected");
  });

  // Listen for user activity changes (online/offline)
  connection.on("UserActivityChanged", function (data) {
    window.DashboardUI.updateUserActivityUI(data);
  });

  connection.on("DashboardDataUpdated", function () {
    window.DashboardUI.refreshDashboardData();
  });
  // Start connection
  connection
    .start()
    .then(() => {
      isConnected = true;
      updateConnectionStatus("connected");
      startHeartbeat(); // Start sending heartbeats
      
      // Join dashboard_viewers group to receive UserActivityChanged events
      connection.invoke("JoinDashboardViewers")
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
  }

  function stopHeartbeat() {
    if (heartbeatInterval) {
      clearInterval(heartbeatInterval);
      heartbeatInterval = null;
    }
  }

  // Handle page visibility changes
  document.addEventListener("visibilitychange", () => {
    if (!document.hidden && connection.state !== signalR.HubConnectionState.Connected) {
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
