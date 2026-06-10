(function () {
  "use strict";

  if (typeof signalR === "undefined") {
    console.warn("SignalR library not loaded - notifications will not work in real-time");
    return;
  }

  // Define global namespace
  window.notificationSystem = window.notificationSystem || {};
  const ns = window.notificationSystem;
  
  // State
  ns.state = {
    connection: null,
    isConnected: false,
    unreadCount: 0,
    pendingApprovalCount: 0,
    notifications: []
  };

  ns.getUnreadCount = () => ns.state.unreadCount;
  ns.getPendingApprovalCount = () => ns.state.pendingApprovalCount;

  ns.initNotificationConnection = function() {
    ns.state.connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/dashboard")
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    ns.state.connection.on("ReceiveNotification", function (data) {
      console.log("📬 New notification received:", data);
      ns.handleNewNotification(data);
    });

    ns.state.connection.on("UpdateNotificationBadge", function (data) {
      console.log("🔢 Badge count updated:", data.unreadCount);
      ns.updateBadgeCount(data.unreadCount);
    });

    ns.state.connection.on("UpdateApprovalBadge", function (data) {
      console.log("🔔 Approval badge updated:", data.pendingCount);
      if (data.pendingCount === -1) {
        ns.loadPendingApprovalCount();
      } else {
        ns.updateApprovalBadge(data.pendingCount);
      }
    });

    ns.state.connection.start()
      .then(() => {
        console.log("✅ Notification SignalR connected");
        ns.state.isConnected = true;
        ns.loadNotifications();
        ns.loadPendingApprovalCount();
      })
      .catch((error) => {
        console.error("❌ Failed to connect to notification hub:", error);
        setTimeout(ns.initNotificationConnection, 5000);
      });

    ns.state.connection.onreconnected(() => {
      console.log("🔄 Notification SignalR reconnected");
      ns.state.isConnected = true;
      ns.loadNotifications();
      ns.loadPendingApprovalCount();
    });

    ns.state.connection.onclose(() => {
      console.log("❌ Notification SignalR disconnected");
      ns.state.isConnected = false;
    });
  };

  ns.handleNewNotification = function(data) {
    ns.state.notifications.unshift({
      notificationId: data.notificationId,
      title: data.title,
      message: data.message,
      notificationType: data.notificationType,
      actionUrl: data.actionUrl,
      createdAt: data.createdAt,
      isRead: false,
    });

    ns.state.unreadCount = data.unreadCount || ns.state.unreadCount + 1;
    ns.updateBadgeCount(ns.state.unreadCount);
    ns.updateNotificationList();
    ns.showToastNotification(data);

    if (data.notificationType === "ApprovalRequest") {
      ns.playNotificationSound();
      ns.loadPendingApprovalCount();
    }
    ns.showBrowserNotification(data);
  };

  ns.loadNotifications = async function(loadAll = false) {
    try {
      const take = loadAll ? 100 : 5;
      const response = await fetch(`/api/notifications?take=${take}`);
      if (!response.ok) throw new Error("Failed to load notifications");

      const data = await response.json();
      ns.state.notifications = data.notifications || [];
      ns.state.unreadCount = data.unreadCount || 0;

      ns.updateBadgeCount(ns.state.unreadCount);
      ns.updateNotificationList();
    } catch (error) {
      console.error("Failed to load notifications:", error);
    }
  };

  ns.loadPendingApprovalCount = async function() {
    try {
      const response = await fetch("/api/approvals/pending-count");
      if (!response.ok) return;

      const data = await response.json();
      ns.updateApprovalBadge(data.pendingCount || 0);
    } catch (error) {
      console.error("Failed to load pending approval count:", error);
    }
  };

  ns.markAsRead = async function(notificationId) {
    try {
      const response = await fetch(`/api/notifications/${notificationId}/read`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });

      if (response.ok) {
        const result = await response.json();
        ns.updateBadgeCount(result.unreadCount);
        const notification = ns.state.notifications.find((n) => n.notificationId === notificationId);
        if (notification) notification.isRead = true;
        ns.updateNotificationList();
      }
    } catch (error) {
      console.error("Failed to mark notification as read:", error);
    }
  };

  ns.markAllAsRead = async function() {
    try {
      const response = await fetch("/api/notifications/read-all", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });

      if (response.ok) {
        ns.updateBadgeCount(0);
        ns.state.notifications.forEach((n) => (n.isRead = true));
        ns.updateNotificationList();
      }
    } catch (error) {
      console.error("Failed to mark all notifications as read:", error);
    }
  };

})();
