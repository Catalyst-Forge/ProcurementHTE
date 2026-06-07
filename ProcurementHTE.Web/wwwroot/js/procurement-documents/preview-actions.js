(function (window) {
    const docs = window.ProcurementDocuments;

    function setModalAccessibility(modalEl) {
        if (!modalEl || modalEl.dataset.accessibilityBound) return;
        modalEl.dataset.accessibilityBound = '1';
        modalEl.addEventListener('hide.bs.modal', () => {
            const focused = modalEl.querySelector(':focus');
            if (focused && typeof focused.blur === 'function') focused.blur();
        });
        modalEl.addEventListener('hidden.bs.modal', () => {
            modalEl.setAttribute('aria-hidden', 'true');
            modalEl.removeAttribute('aria-modal');
            modalEl.removeAttribute('role');
        });
    }

    function openPdfModal(title, fileName, url, hasRealFile) {
        const modalEl = document.getElementById('pdfPreviewModal');
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        const iframe = document.getElementById('pdfPreviewFrame');
        const loading = document.getElementById('pdfLoadingContainer');
        const errorBox = document.getElementById('pdfErrorContainer');
        const downloadBtn = document.getElementById('downloadFromPreview');

        setModalAccessibility(modalEl);
        document.getElementById('previewModalTitle').textContent = title || 'Preview Dokumen';
        document.getElementById('previewFileName').textContent = fileName || '';
        downloadBtn.classList.toggle('d-none', !hasRealFile);

        iframe.style.display = 'none';
        iframe.src = 'about:blank';
        loading.classList.remove('d-none');
        errorBox.classList.add('d-none');

        modal.show();
        modalEl?.removeAttribute('aria-hidden');
        modalEl?.setAttribute('aria-modal', 'true');
        modalEl?.setAttribute('role', 'dialog');

        iframe.src = url;
        iframe.onload = () => {
            loading.classList.add('d-none');
            iframe.style.display = 'block';
        };
        iframe.onerror = () => {
            loading.classList.add('d-none');
            errorBox.classList.remove('d-none');
        };
    }

    function initPreviewHandlers() {
        docs.bindOnce('.btn-preview', 'boundPreview', btn => {
            btn.addEventListener('click', async function () {
                const docId = this.dataset.docId;
                docs.state.currentDownloadDocId = docId;
                docs.state.currentPreviewUrl = null;

                try {
                    const url = `/ProcurementDocuments/PreviewUrl/${encodeURIComponent(docId)}?procurementId=${encodeURIComponent(docs.procurementId())}`;
                    const res = await fetch(url);
                    const data = await res.json();
                    if (!data?.ok || !data?.url) throw new Error(data?.error || 'URL preview tidak tersedia');

                    docs.state.currentPreviewUrl = data.url;
                    openPdfModal(this.dataset.docName, this.dataset.fileName, data.url, true);
                } catch (err) {
                    console.error('Preview(existing) error:', err);
                    docs.showToast('error', 'Failed to load preview', err.message, 3500);
                }
            });
        });
    }

    function initGeneratePreviewHandlers() {
        docs.bindOnce('.btn-preview-generate', 'boundPreviewGenerate', btn => {
            btn.addEventListener('click', async function () {
                const procurementId = this.dataset.procurementId;
                const docTypeId = this.dataset.docTypeId;
                const docName = this.dataset.docName || 'Preview Template';
                if (!procurementId || !docTypeId) {
                    docs.showToast('error', 'Data tidak lengkap', 'procurementId / documentTypeId kosong.', 3500);
                    return;
                }

                docs.state.currentDownloadDocId = null;
                docs.state.currentPreviewUrl = null;
                const query = `procurementId=${encodeURIComponent(procurementId)}&documentTypeId=${encodeURIComponent(docTypeId)}`;

                try {
                    const res = await fetch(`/ProcurementDocuments/PreviewGeneratedUrl?${query}`);
                    if (res.ok) {
                        const data = await res.json().catch(() => null);
                        if (data?.ok && data?.url) {
                            docs.state.currentPreviewUrl = data.url;
                            openPdfModal(`Preview Template - ${docName}`, '', data.url, false);
                            return;
                        }
                    }
                    openPdfModal(`Preview Template - ${docName}`, '', `/ProcurementDocuments/PreviewGenerated?${query}`, false);
                } catch (err) {
                    console.error('Preview(template) error:', err);
                    docs.showToast('error', 'Failed to load template preview', err.message, 3500);
                }
            });
        });
    }

    function initDownloadHandlers() {
        docs.bindOnce('.btn-download', 'boundDownload', btn => {
            btn.addEventListener('click', function () {
                const docId = this.dataset.docId;
                const fileName = this.dataset.fileName;
                docs.showToast('info', 'Memproses download...', '', 1500);

                const a = document.createElement('a');
                a.href = `/ProcurementDocuments/Download/${encodeURIComponent(docId)}?procurementId=${encodeURIComponent(docs.procurementId())}`;
                if (fileName) a.download = fileName;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                setTimeout(() => docs.showToast('success', 'Download dimulai', fileName || '', 2500), 400);
            });
        });
    }

    function initDeleteHandlers() {
        docs.bindOnce('.btn-delete', 'boundDelete', btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const docName = this.dataset.docName || '-';
                const fileName = this.dataset.fileName || '';

                Swal.fire({
                    title: 'Confirm Document Deletion',
                    html: `<div class="text-start"><p class="mb-2">Are you sure you want to delete this document?</p><div class="alert alert-warning mb-0"><strong>${docName}</strong><br><small class="text-muted">${fileName}</small></div></div>`,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#dc3545',
                    cancelButtonColor: '#6c757d',
                    confirmButtonText: '<i class="bi bi-trash me-1"></i> Yes, Delete',
                    cancelButtonText: 'Cancel',
                    reverseButtons: true,
                    focusCancel: true
                }).then(result => {
                    if (!result.isConfirmed) return;
                    document.getElementById('deleteDocId').value = this.dataset.docId;
                    document.getElementById('deleteProcurementId').value = this.dataset.procurementId;
                    document.getElementById('deleteForm').submit();
                });
            });
        });
    }

    function initPreviewDownloadButton() {
        const btn = document.getElementById('downloadFromPreview');
        if (!btn || btn.dataset.boundPreviewDownload) return;
        btn.dataset.boundPreviewDownload = '1';
        btn.addEventListener('click', () => {
            if (!docs.state.currentDownloadDocId) {
                docs.showToast('info', 'Download not available yet', 'Generate or upload the document first.', 3000);
                return;
            }
            document.querySelector(`.btn-download[data-doc-id="${docs.state.currentDownloadDocId}"]`)?.click();
        });
    }

    docs.onBoot(() => {
        initPreviewHandlers();
        initGeneratePreviewHandlers();
        initDownloadHandlers();
        initDeleteHandlers();
        initPreviewDownloadButton();
    });
})(window);
