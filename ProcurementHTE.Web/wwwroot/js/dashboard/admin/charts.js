(function (window, document) {
    function chartConfig() {
        return window.adminDashboardConfig || {};
    }

    function setChartDefaults() {
        if (typeof Chart === 'undefined') return;
        Chart.defaults.font.family = "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif";
    }

    function renderRoleChart() {
        const roleData = chartConfig().roleDistribution || [];
        const canvas = document.getElementById('roleDistributionChart');
        if (!canvas || typeof Chart === 'undefined' || roleData.length === 0) return;

        new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: roleData.map(item => item.roleName),
                datasets: [{
                    label: 'Users',
                    data: roleData.map(item => item.userCount),
                    backgroundColor: roleData.map(item => item.color),
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { boxWidth: 12, padding: 8, font: { size: 10 } }
                    }
                }
            }
        });
    }

    function renderRegionChart() {
        const regionData = chartConfig().regionDistribution || [];
        const canvas = document.getElementById('regionChart');
        if (!canvas || typeof Chart === 'undefined' || regionData.length === 0) return;

        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: regionData.map(item => item.regionName),
                datasets: [{
                    label: 'Count',
                    data: regionData.map(item => item.count),
                    backgroundColor: ['#0d6efd', '#198754', '#ffc107', '#dc3545', '#0dcaf0', '#6f42c1'],
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: { x: { beginAtZero: true } }
            }
        });
    }

    function updateDateTime() {
        const element = document.getElementById('currentDateTime');
        if (!element) return;

        const options = {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        };
        element.textContent = new Date().toLocaleDateString('id-ID', options);
    }

    document.addEventListener('DOMContentLoaded', () => {
        setChartDefaults();
        renderRoleChart();
        renderRegionChart();
        updateDateTime();
        setInterval(updateDateTime, 60000);
    });
})(window, document);
