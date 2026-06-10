(function (window) {
    const details = window.PrDetails;

    function renderFileCell(result) {
        const size = Number(result.fileSize || 0);
        const sizeText = size > 0
            ? ` <span class="text-muted small">&bull; ${details.formatSize(size)}</span>`
            : '';

        return `<small class="text-muted"><i class="bi bi-file-pdf text-danger"></i> ${details.escapeHtml(result.fileName || '')}</small>${sizeText}`;
    }

    function updateRowAfterGeneration(form, result) {
        const row = form.closest('tr');
        if (!row || !result.procDocumentId) return;

        const procurementId = form.querySelector('input[name="procurementId"]')?.value || '';
        const documentTypeId = form.querySelector('input[name="documentTypeId"]')?.value || '';
        const prId = form.querySelector('input[name="prId"]')?.value || details.prId();
        const statusCell = row.querySelector('td:nth-child(4)');
        const fileCell = row.querySelector('td:nth-child(5)');
        const actionsCell = row.querySelector('td[data-actions-row]');

        form.closest('div')?.remove();
        if (statusCell) {
            statusCell.innerHTML = '<span class="badge rounded-pill bg-success bg-opacity-25 text-success py-2 px-3"><i class="bi bi-check-lg"></i> Uploaded</span>';
        }
        if (fileCell && result.fileName) fileCell.innerHTML = renderFileCell(result);
        if (!actionsCell) return;

        const wrapper = details.ensureActionsWrapper(actionsCell);
        wrapper.insertAdjacentHTML('beforeend', details.renderActionButtons({
            documentId: result.procDocumentId,
            documentName: result.documentTypeName || '',
            fileName: result.fileName || '',
            procurementId,
            documentTypeId,
            isGenerated: true,
            prId
        }));
        details.boot();
    }

    async function submitGenerateForm(form, button) {
        if (button.dataset.loading === 'true') return;

        button.dataset.loading = 'true';
        button.dataset.originalHtml = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Processing...';

        try {
            const response = await fetch('/PurchaseRequisitions/GenerateDocumentAjax', {
                method: 'POST',
                headers: { RequestVerificationToken: details.token() },
                body: new FormData(form)
            });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message || 'Failed to generate document');

            details.showToast('success', 'Success', result.message);
            updateRowAfterGeneration(form, result);
        } catch (error) {
            button.innerHTML = button.dataset.originalHtml;
            details.showToast('error', 'Error', error.message);
        } finally {
            button.disabled = false;
            button.dataset.loading = 'false';
        }
    }

    function initGenerateForms() {
        details.bindOnce('form[data-generate-form="true"]', 'boundGenerateForm', form => {
            const button = form.querySelector('.js-generate-btn');
            if (!button) return;

            form.addEventListener('submit', event => {
                event.preventDefault();
                submitGenerateForm(form, button);
            });
        });
    }

    details.onBoot(initGenerateForms);
})(window);
