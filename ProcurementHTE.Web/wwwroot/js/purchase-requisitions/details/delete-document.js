(function (window) {
    const details = window.PrDetails;

    function resetRowAfterDelete(row, button, payload) {
        const actionsCell = row.querySelector('td[data-actions-row]');
        const statusCell = row.querySelector('td:nth-child(4)');
        const fileCell = row.querySelector('td:nth-child(5)');
        const typeCell = row.querySelector('td:nth-child(3)');
        const isMandatory = typeCell && typeCell.textContent.includes('Required');

        if (statusCell) {
            statusCell.innerHTML = isMandatory
                ? '<span class="badge rounded-pill bg-warning bg-opacity-25 text-warning-emphasis py-2 px-3"><i class="bi bi-exclamation-lg"></i> Required</span>'
                : '<span class="badge rounded-pill bg-light text-dark border py-2 px-3">Optional</span>';
        }
        if (fileCell) fileCell.innerHTML = '<span class="text-muted">-</span>';
        if (!actionsCell) return;

        const rowIndex = actionsCell.dataset.actionsRow;
        const wrapper = details.ensureActionsWrapper(actionsCell);
        button.closest('div')?.remove();

        const context = {
            rowIndex,
            procurementId: payload.procurementId,
            documentTypeId: button.dataset.documentTypeId || '',
            prId: button.dataset.prId || details.prId()
        };

        const replacement = button.dataset.isGenerated === 'true'
            ? details.renderGenerateForm(context)
            : details.renderUploadForm(context);

        wrapper.insertAdjacentHTML('afterbegin', replacement);
        details.boot();
    }

    async function deleteDocument(button) {
        const result = await Swal.fire({
            title: 'Delete Document?',
            html: `<p>Are you sure you want to delete <strong>${button.dataset.docName || ''}</strong>?</p>`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            confirmButtonText: '<i class="bi bi-trash"></i> Delete',
            cancelButtonText: 'Cancel',
            reverseButtons: true,
            focusCancel: true
        });
        if (!result.isConfirmed) return;

        const originalHtml = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

        try {
            const formData = new FormData();
            formData.append('id', button.dataset.docId);
            formData.append('procurementId', button.dataset.procurementId);
            formData.append('documentTypeId', button.dataset.documentTypeId || '');

            const response = await fetch('/ProcurementDocuments/DeleteAjax', {
                method: 'POST',
                headers: { RequestVerificationToken: details.token() },
                body: formData
            });

            const text = await response.text();
            const payload = JSON.parse(text);
            if (!response.ok || !payload.ok) throw new Error(payload.error || 'Delete failed');

            details.showToast('success', 'Document deleted', button.dataset.docName || '');
            details.updateDocumentCounts(payload);

            const row = button.closest('tr');
            if (row) resetRowAfterDelete(row, button, payload);
        } catch (error) {
            button.disabled = false;
            button.innerHTML = originalHtml;
            details.showToast('error', 'Delete failed', error.message);
        }
    }

    function initDeleteHandlers() {
        details.bindOnce('.btn-delete', 'boundDelete', button => {
            button.addEventListener('click', function (event) {
                event.preventDefault();
                deleteDocument(this);
            });
        });
    }

    details.onBoot(initDeleteHandlers);
})(window);
