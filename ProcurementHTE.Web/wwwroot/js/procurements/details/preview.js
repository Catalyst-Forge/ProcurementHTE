(function (window, document) {
    const details = window.ProcurementDetails;

    function setPreviewState(isLoading, hasError) {
        document.getElementById('pdfLoadingContainer')?.classList.toggle('d-none', !isLoading);
        document.getElementById('pdfErrorContainer')?.classList.toggle('d-none', !hasError);

        const frame = document.getElementById('pdfPreviewFrame');
        if (frame) frame.style.display = isLoading || hasError ? 'none' : 'block';
    }

    function prepareModal(button) {
        const modalElement = document.getElementById('pdfPreviewModal');
        const frame = document.getElementById('pdfPreviewFrame');
        if (!modalElement || !frame) return null;

        document.getElementById('previewModalTitle').textContent = `Preview Dokumen: ${button.dataset.docName || ''}`;
        document.getElementById('previewFileName').textContent = button.dataset.fileName || '';
        frame.src = '';
        setPreviewState(true, false);
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
        return frame;
    }

    async function openPreview(button) {
        const docId = button.dataset.docId;
        if (!docId) return;

        const frame = prepareModal(button);
        if (!frame) return;

        details.state.currentDownloadDocId = docId;
        details.state.currentPreviewUrl = null;

        try {
            const response = await fetch(details.previewUrl(docId), {
                headers: {
                    Accept: 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });
            const payload = await response.json();
            if (!payload.ok || !payload.url) throw new Error(payload.error || 'Failed to get preview URL');

            details.state.currentPreviewUrl = payload.url;
            frame.onload = () => setPreviewState(false, false);
            frame.onerror = () => setPreviewState(false, true);
            frame.src = payload.url;
        } catch {
            setPreviewState(false, true);
        }
    }

    function initPreviewHandlers() {
        document.querySelectorAll('.btn-preview').forEach(button => {
            if (button.dataset.boundPreview) return;
            button.dataset.boundPreview = '1';
            button.addEventListener('click', () => openPreview(button));
        });

        const download = document.getElementById('downloadFromPreview');
        if (!download || download.dataset.boundPreviewDownload) return;
        download.dataset.boundPreviewDownload = '1';
        download.addEventListener('click', () => {
            if (!details.state.currentDownloadDocId) return;
            document.querySelector(`.btn-download[data-doc-id='${details.state.currentDownloadDocId}']`)?.click();
        });
    }

    details.onBoot(initPreviewHandlers);
})(window, document);
