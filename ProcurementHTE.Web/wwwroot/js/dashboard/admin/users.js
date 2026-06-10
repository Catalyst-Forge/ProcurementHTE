(function (window, document) {
    async function loadOnlineUsers() {
        try {
            const response = await fetch('/Dashboard/Admin/GetOnlineUsers');
            if (!response.ok) throw new Error('Failed to fetch online users');

            const data = await response.json();
            if (!data || !Array.isArray(data.users)) return;

            document.querySelector('[data-online-users-count]')?.replaceChildren(String(data.onlineCount || 0));
            document.querySelector('[data-offline-users-count]')?.replaceChildren(String(data.offlineCount || 0));
            updateUserActivityTable(data.users);
        } catch {
            // Keep dashboard stable when realtime data cannot be fetched.
        }
    }

    function updateUserActivityTable(users) {
        const activityTable = document.querySelector('[data-user-activity-table]');
        if (!activityTable || !Array.isArray(users) || users.length === 0) return;

        users.forEach(user => updateUserRow(activityTable, user));
    }

    function updateUserRow(activityTable, user) {
        if (!user || !user.id) return;

        const row = activityTable.querySelector(`tr[data-user-id="${user.id}"]`);
        if (!row) return;

        const isOnline = user.isOnline || false;
        if (row.dataset.userStatus === (isOnline ? 'online' : 'offline')) return;

        row.dataset.userStatus = isOnline ? 'online' : 'offline';
        updateStatusIcon(row, isOnline);
        updateStatusBadge(row, isOnline);
        updateLastActivity(row, user, isOnline);
    }

    function updateStatusIcon(row, isOnline) {
        const iconCell = row.querySelector('.user-status-icon')?.closest('td');
        if (!iconCell) return;

        iconCell.innerHTML = isOnline
            ? `<div class="position-relative d-inline-block">
                   <i class="bi bi-person-fill text-success fs-5 user-status-icon"></i>
                   <span class="position-absolute top-0 start-100 translate-middle p-1 bg-success border border-light rounded-circle">
                       <span class="visually-hidden">Online</span>
                   </span>
               </div>`
            : '<i class="bi bi-person text-secondary fs-5 user-status-icon"></i>';
    }

    function updateStatusBadge(row, isOnline) {
        const badge = row.querySelector('.user-status-badge');
        if (!badge) return;

        badge.className = isOnline
            ? 'badge bg-success user-status-badge'
            : 'badge bg-secondary user-status-badge';
        badge.innerHTML = isOnline
            ? '<i class="bi bi-circle-fill admin-dot"></i> Online'
            : '<i class="bi bi-circle admin-dot"></i> Offline';
    }

    function updateLastActivity(row, user, isOnline) {
        const cell = row.querySelector('.user-last-activity');
        if (!cell) return;

        cell.className = isOnline ? 'text-success user-last-activity' : 'text-muted user-last-activity';
        if (user.connectionInfo?.lastActivityAt) {
            cell.innerHTML = `<i class="bi bi-clock"></i> ${formatDateTime(user.connectionInfo.lastActivityAt)}`;
        } else if (isOnline) {
            cell.innerHTML = '<i class="bi bi-clock"></i> Online sekarang';
        }
    }

    function formatDateTime(dateString) {
        if (!dateString) return '-';

        const date = new Date(dateString);
        const diff = Math.floor((new Date() - date) / 1000);
        if (diff < 60) return 'Baru saja';
        if (diff < 3600) return `${Math.floor(diff / 60)} menit yang lalu`;
        if (diff < 86400) return `${Math.floor(diff / 3600)} jam yang lalu`;

        return date.toLocaleDateString('id-ID', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        setTimeout(loadOnlineUsers, 1000);
        setInterval(loadOnlineUsers, 30000);
    });

    if (window.dashboardConnection) {
        window.dashboardConnection.on('UserActivityChanged', loadOnlineUsers);
    }
})(window, document);
