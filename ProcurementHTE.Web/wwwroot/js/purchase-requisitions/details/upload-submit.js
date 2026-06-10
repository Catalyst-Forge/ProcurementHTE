(function (window) {
    const details = window.PrDetails;

    function setFeedback(row, text, className) {
        const feedback = document.querySelector(`.upload-feedback[data-row="${row}"]`);
        if (!feedback) return;
        feedback.classList.remove('text-muted', 'text-danger', 'text-success');
        feedback.classList.add(className);
        feedback.textContent = text;
    }

    function renderFileCell(document) {
        const name = details.escapeHtml(document?.name || '');
        const size = Number(document?.size || 0);
        const sizeText = size > 0
            ? ` <span class="text-muted small">&bull; ${details.formatSize(size)}</span>`
            : '';

        return `<small class="text-muted"><i class="bi bi-file-pdf text-danger"></i> ${name}</small>${sizeText}`;
    }

    function updateRowAfterUpload(form, payload) {
        const row = form.closest('tr');
        if (!row || !payload?.document?.id) return;

        const procurementId = form.querySelector('input[name="ProcurementId"]')?.value || '';
        const documentTypeId = form.querySelector('input[name="DocumentTypeId"]')?.value || '';
        const statusCell = row.querySelector('td:nth-child(4)');
        const fileCell = row.querySelector('td:nth-child(5)');
        const actionsCell = row.querySelector('td[data-actions-row]');

        if (statusCell) {
            statusCell.innerHTML = '<span class="badge rounded-pill bg-success bg-opacity-25 text-success py-2 px-3"><i class="bi bi-check-lg"></i> Uploaded</span>';
        }
        if (fileCell) fileCell.innerHTML = renderFileCell(payload.document);
        if (!actionsCell) return;

        const wrapper = details.ensureActionsWrapper(actionsCell);
        const actions = details.renderActionButtons({
            documentId: payload.document.id,
            documentName: payload.document.name,
            fileName: payload.document.name,
            procurementId,
            documentTypeId,
            isGenerated: false,
            prId: details.prId()
        });

        form.remove();
        wrapper.insertAdjacentHTML('beforeend', actions);
        details.boot();
    }

    async function submitUploadForm(form) {
        const row = form.dataset.row;
        const input = form.querySelector('.file-input');
        const button = form.querySelector('.upload-btn');

        if (!input?.files?.length) {
            details.showToast('warning', 'No file selected');
            return;
        }

        const original = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Uploading...';
        setFeedback(row, 'Uploading...', 'text-muted');

        try {
            const response = await fetch(form.action, {
                method: form.method || 'POST',
                body: new FormData(form),
                headers: {
                    Accept: 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });
            const payload = await response.json();
            if (!response.ok || payload?.ok === false) throw new Error(payload?.error || 'Upload failed');

            details.showToast('success', 'Upload successful', payload.document?.name || '');
            details.updateDocumentCounts(payload);
            updateRowAfterUpload(form, payload);
        } catch (error) {
            button.disabled = false;
            button.innerHTML = original;
            setFeedback(row, error.message, 'text-danger');
            details.showToast('error', 'Upload failed', error.message);
        }
    }

    function initUploadForms() {
        details.bindOnce('form[data-upload-form="true"]', 'boundUploadForm', form => {
            form.addEventListener('submit', event => {
                event.preventDefault();
                submitUploadForm(form);
            });
        });
    }

    details.onBoot(initUploadForms);
})(window);
