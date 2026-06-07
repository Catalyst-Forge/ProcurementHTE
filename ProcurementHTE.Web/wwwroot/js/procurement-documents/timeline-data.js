(function (window) {
    const docs = window.ProcurementDocuments;

    function normalizeTimelinePayload(raw) {
        const d = raw || {};
        const norm = {
            procurementId: d.ProcurementId ?? d.procurementId ?? null,
            procDocumentId: d.ProcDocumentId ?? d.procDocumentId ?? null,
            docStatus: d.DocStatus ?? d.docStatus ?? null,
            currentGateLevel: d.CurrentGateLevel ?? d.currentGateLevel ?? null,
            requiredRoles: Array.isArray(d.RequiredRoles) ? d.RequiredRoles : (Array.isArray(d.requiredRoles) ? d.requiredRoles : []),
            steps: Array.isArray(d.Steps) ? d.Steps : (Array.isArray(d.steps) ? d.steps : [])
        };

        norm.steps = norm.steps.map(step => ({
            procDocumentApprovalId: step.ProcDocumentApprovalId ?? step.procDocumentApprovalId ?? null,
            level: step.Level ?? step.level ?? 0,
            roleId: step.RoleId ?? step.roleId ?? null,
            roleName: step.RoleName ?? step.roleName ?? null,
            status: step.Status ?? step.status ?? null,
            approverUserId: step.ApproverUserId ?? step.approverUserId ?? null,
            approverFullName: step.ApproverFullName ?? step.approverFullName ?? null,
            approvedAt: step.ApprovedAt ?? step.approvedAt ?? null,
            note: step.Note ?? step.note ?? null
        }));

        norm.requiredRoles = norm.requiredRoles.map(role => ({
            roleId: role.RoleId ?? role.roleId ?? null,
            roleName: role.RoleName ?? role.roleName ?? null,
            procDocumentApprovalId: role.ProcDocumentApprovalId ?? role.procDocumentApprovalId ?? null,
            level: role.Level ?? role.level ?? null,
            status: role.Status ?? role.status ?? null,
            note: role.Note ?? role.note ?? null,
            approverId: role.ApproverId ?? role.approverId ?? null,
            approverFullName: role.ApproverFullName ?? role.approverFullName ?? null,
            approvedAt: role.ApprovedAt ?? role.approvedAt ?? null
        }));

        return norm;
    }

    function resetTimelineModal(docName) {
        document.getElementById('approvalDocName').textContent = docName || '';
        document.getElementById('approvalLoading').classList.remove('d-none');
        document.getElementById('approvalError').classList.add('d-none');
        document.getElementById('approvalTimelineBody').classList.add('d-none');
    }

    function openTimelineModal(docId, docName) {
        resetTimelineModal(docName);
        new bootstrap.Modal(document.getElementById('approvalTimelineModal')).show();

        fetch(`/ProcurementDocuments/ApprovalTimeline/${encodeURIComponent(docId)}`)
            .then(response => {
                if (!response.ok) throw new Error('HTTP ' + response.status);
                return response.json();
            })
            .then(payload => {
                if (!payload?.ok || !payload?.data) {
                    throw new Error(payload?.message || 'Bad payload');
                }

                docs.renderTimeline(normalizeTimelinePayload(payload.data), docId);
            })
            .catch(error => {
                console.error(error);
                document.getElementById('approvalLoading').classList.add('d-none');
                document.getElementById('approvalError').classList.remove('d-none');
            });
    }

    function initTimelineHandlers() {
        docs.bindOnce('.btn-timeline', 'boundTimeline', btn => {
            btn.addEventListener('click', function () {
                openTimelineModal(this.dataset.docId, this.dataset.docName || '');
            });
        });
    }

    docs.onBoot(initTimelineHandlers);
})(window);
