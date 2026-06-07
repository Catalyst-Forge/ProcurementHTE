(function (window) {
    const docs = window.ProcurementDocuments;

    function statusClass(status) {
        const lower = (status || '').toLowerCase();
        if (lower.includes('pending')) return 'bg-warning text-dark';
        if (lower.includes('approved')) return 'bg-success';
        if (lower.includes('reject')) return 'bg-danger';
        if (lower.includes('generated') || lower.includes('uploaded')) return 'bg-info';
        return 'bg-secondary';
    }

    function stepAppearance(status) {
        const lower = (status || '').toLowerCase();
        if (lower === 'approved') return { icon: 'check2-circle', color: 'success', badge: '' };
        if (lower === 'rejected') return { icon: 'x-circle', color: 'danger', badge: '' };
        if (lower === 'pending') return { icon: 'hourglass-split', color: 'warning', badge: 'text-dark' };
        return { icon: 'lock', color: 'secondary', badge: '' };
    }

    function renderEmptyTimeline(loading, body, stepper) {
        loading.classList.add('d-none');
        document.getElementById('approvalError')?.classList.add('d-none');
        body.classList.remove('d-none');
        stepper.innerHTML = `<li class="timeline-item">
            <div class="d-flex">
                <div class="timeline-icon text-secondary"><i class="bi bi-lock"></i></div>
                <div class="flex-grow-1">
                    <div class="fw-semibold">No approval data available</div>
                    <div class="small text-muted">Pastikan flow sudah tergenerate & API mengembalikan steps.</div>
                </div>
            </div>
        </li>`;
    }

    function renderStep(step, stepper) {
        const look = stepAppearance(step.status);
        const approvedAt = step.approvedAt
            ? ` - <i class="bi bi-clock-history"></i> ${new Date(step.approvedAt).toLocaleString()}`
            : '';
        const approver = step.approverFullName
            ? `<i class="bi bi-person-badge"></i> ${step.approverFullName}`
            : '';
        const note = step.note ? ` - <i class="bi bi-chat-text"></i> ${docs.escapeHtml(step.note)}` : '';

        const li = document.createElement('li');
        li.className = 'timeline-item';
        li.innerHTML = `
            <div class="d-flex">
                <div class="timeline-icon text-${look.color}">
                    <i class="bi bi-${look.icon}"></i>
                </div>
                <div class="flex-grow-1">
                    <div class="d-flex align-items-center flex-wrap gap-2 mb-1">
                        <span class="badge bg-light text-dark border">L${step.level}</span>
                        <span class="fw-semibold">${step.roleName ?? '-'}</span>
                        <span class="badge rounded-pill bg-${look.color} ${look.badge} text-capitalize">${step.status ?? '-'}</span>
                    </div>
                    <div class="small text-muted">${approver}${approvedAt}${note}</div>
                </div>
            </div>`;
        stepper.appendChild(li);
    }

    function renderRequiredRoles(data, docIdForInline, nextWrap, nextChips) {
        if (!Array.isArray(data?.requiredRoles) || data.requiredRoles.length === 0) return;

        nextWrap.style.display = '';
        data.requiredRoles.forEach(role => {
            const span = document.createElement('span');
            span.className = 'badge rounded-pill bg-light text-dark border me-1';
            span.textContent = role.roleName || '-';
            nextChips.appendChild(span);
        });

        const inline = document.getElementById('next-' + (data.procDocumentId || docIdForInline));
        if (!inline) return;

        const names = data.requiredRoles.map(role => role.roleName || '-').join(', ');
        inline.innerHTML = `<span class="badge bg-light text-dark border"><i class="bi bi-person-check me-1"></i>Next: ${names}</span>`;
    }

    docs.renderTimeline = function (data, docIdForInline) {
        const body = document.getElementById('approvalTimelineBody');
        const stepper = document.getElementById('approvalStepper');
        const loading = document.getElementById('approvalLoading');
        const nextWrap = document.getElementById('nextApproverWrap');
        const nextChips = document.getElementById('nextApproverChips');
        const docStatusBadge = document.getElementById('docStatusBadge');
        const currentGateBadge = document.getElementById('currentGateBadge');

        stepper.innerHTML = '';
        nextChips.innerHTML = '';
        nextWrap.style.display = 'none';
        currentGateBadge.classList.add('d-none');

        const status = data?.docStatus || '-';
        docStatusBadge.className = 'badge rounded-pill ' + statusClass(status);
        docStatusBadge.textContent = 'Status: ' + status;

        if (data?.currentGateLevel) {
            currentGateBadge.classList.remove('d-none');
            currentGateBadge.textContent = `Gate: L${data.currentGateLevel}`;
        }

        const steps = Array.isArray(data?.steps) ? data.steps.slice() : [];
        steps.sort((a, b) => (a.level - b.level) || ((a.roleName || '').localeCompare(b.roleName || '')));
        if (steps.length === 0) {
            renderEmptyTimeline(loading, body, stepper);
            return;
        }

        steps.forEach(step => renderStep(step, stepper));
        renderRequiredRoles(data, docIdForInline, nextWrap, nextChips);
        loading.classList.add('d-none');
        document.getElementById('approvalError')?.classList.add('d-none');
        body.classList.remove('d-none');
    };
})(window);
