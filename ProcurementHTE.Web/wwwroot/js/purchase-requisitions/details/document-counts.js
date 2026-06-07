(function (window) {
    const details = window.PrDetails;

    function updateBadgeColor(badge, allDocsReady) {
        if (allDocsReady) {
            badge.classList.remove('bg-warning');
            badge.classList.add('bg-success');
        } else {
            badge.classList.remove('bg-success');
            badge.classList.add('bg-warning');
        }
    }

    details.updateDocumentCounts = function (payload) {
        if (!payload?.procurementId) return;
        if (typeof payload.uploadedDocs === 'undefined' || typeof payload.totalDocs === 'undefined') return;

        const uploaded = payload.uploadedDocs;
        const total = payload.totalDocs;
        const allDocsReady = uploaded >= total && total > 0;
        const badge = document.getElementById('doc-count-' + payload.procurementId);

        if (badge) {
            badge.textContent = uploaded + '/' + total;
            badge.dataset.uploaded = uploaded;
            badge.dataset.total = total;
            updateBadgeColor(badge, allDocsReady);
        }

        const sendApproval = document.getElementById('send-approval-btn-' + payload.procurementId);
        if (sendApproval) sendApproval.disabled = !allDocsReady;

        const linkedBadge = document.getElementById('linked-doc-count-' + payload.procurementId);
        if (linkedBadge) linkedBadge.textContent = uploaded + '/' + total;
    };
})(window);
