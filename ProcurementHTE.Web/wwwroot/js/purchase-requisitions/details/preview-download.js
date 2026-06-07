(function (window) {
    const details = window.PrDetails;

    function openPdfModal(title, fileName, url, hasRealFile) {
        const modalEl = document.getElementById('pdfPreviewModal');
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        const frame = document.getElementById('pdfPreviewFrame');
        const loading = document.getElementById('pdfLoadingContainer');
        const error = document.getElementById('pdfErrorContainer');
        const download = document.getElementById('downloadFromPreview');

        document.getElementById('previewModalTitle').textContent = title || 'Preview';
        document.getElementById('previewFileName').textContent = fileName || '';
        download.classList.toggle('d-none', !hasRealFile);
        frame.style.display = 'none';
        frame.src = 'about:blank';
        loading.classList.remove('d-none');
        error.classList.add('d-none');
        modal.show();

        frame.src = url;
        frame.onload = () => {
            loading.classList.add('d-none');
            frame.style.display = 'block';
        };
        frame.onerror = () => {
            loading.classList.add('d-none');
            error.classList.remove('d-none');
        };
    }

    function initPreviewHandlers() {
        details.bindOnce('.btn-preview', 'boundPreview', button => {
            button.addEventListener('click', async function () {
                const docId = this.dataset.docId;
                const procurementId = this.closest('tr')?.dataset.procurementId || '';
                details.state.currentDownloadDocId = docId;
                details.state.currentPreviewUrl = null;

                try {
                    const url = `/ProcurementDocuments/PreviewUrl/${encodeURIComponent(docId)}?procurementId=${encodeURIComponent(procurementId)}`;
                    const response = await fetch(url);
                    const payload = await response.json();
                    if (!payload?.ok || !payload?.url) throw new Error(payload?.error || 'URL not available');

                    details.state.currentPreviewUrl = payload.url;
                    openPdfModal(this.dataset.docName, this.dataset.fileName, payload.url, true);
                } catch (error) {
                    details.showToast('error', 'Preview failed', error.message);
                }
            });
        });
    }

    function initDownloadHandlers() {
        details.bindOnce('.btn-download', 'boundDownload', button => {
            button.addEventListener('click', function () {
                const docId = this.dataset.docId;
                const fileName = this.dataset.fileName;
                const procurementId = this.closest('tr')?.dataset.procurementId || '';

                details.showToast('info', 'Downloading...');
                const link = document.createElement('a');
                link.href = `/ProcurementDocuments/Download/${encodeURIComponent(docId)}?procurementId=${encodeURIComponent(procurementId)}`;
                if (fileName) link.download = fileName;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                setTimeout(() => details.showToast('success', 'Download started', fileName || ''), 400);
            });
        });
    }

    function initPreviewDownloadButton() {
        const download = document.getElementById('downloadFromPreview');
        if (!download || download.dataset.boundPreviewDownload) return;
        download.dataset.boundPreviewDownload = '1';
        download.addEventListener('click', () => {
            if (!details.state.currentDownloadDocId) {
                details.showToast('info', 'Download not available');
                return;
            }
            document.querySelector(`.btn-download[data-doc-id="${details.state.currentDownloadDocId}"]`)?.click();
        });
    }

    details.onBoot(() => {
        initPreviewHandlers();
        initDownloadHandlers();
        initPreviewDownloadButton();
    });
})(window);
