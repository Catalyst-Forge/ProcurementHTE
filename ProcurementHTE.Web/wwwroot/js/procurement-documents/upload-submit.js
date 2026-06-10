(function (window) {
    const docs = window.ProcurementDocuments;

    function markUploadSuccess(row, payload) {
        const rowEl = document.querySelector(`tr[data-row-index="${row}"]`);
        const dropzone = document.querySelector(`.upload-dropzone[data-row="${row}"]`);
        const fileInput = document.querySelector(`.file-input[data-row="${row}"]`);
        const label = document.querySelector(`.selected-file[data-row="${row}"]`);
        const statusCell = document.querySelector(`td[data-status-row="${row}"]`);
        const actionsCell = document.querySelector(`td[data-actions-row="${row}"]`);

        if (dropzone) {
            dropzone.classList.add('disabled');
            dropzone.setAttribute('aria-disabled', 'true');
        }
        if (fileInput) {
            fileInput.value = '';
            fileInput.disabled = true;
        }
        if (label) {
            label.classList.remove('text-muted');
            label.classList.add('text-success');
            const fileName = payload?.document?.name || 'Dokumen berhasil diunggah';
            label.innerHTML = `<i class="bi bi-file-earmark-check me-1"></i>${fileName}`;
        }
        if (statusCell) {
            statusCell.innerHTML = `<span class="badge rounded-pill bg-success px-3 py-2">
                <i class="bi bi-check-lg"></i> Uploaded
            </span>`;
        }
        if (actionsCell) actionsCell.classList.add('opacity-75');

        const icon = rowEl?.querySelector('.pt-1 i');
        if (icon) {
            icon.className = 'bi bi-file-earmark-check-fill text-success fs-4';
            icon.setAttribute('title', 'Uploaded');
        }
    }

    function updateProgressIndicators(deltaCompleted) {
        const bar = document.getElementById('procProgressBar');
        if (!bar) return;

        const required = Number(bar.dataset.required || 0);
        const currentCompleted = Number(bar.dataset.completed || 0);
        const nextCompleted = Math.min(required, currentCompleted + deltaCompleted);
        const percent = required > 0 ? Math.min(Math.round((nextCompleted * 100) / required), 100) : 0;

        bar.dataset.completed = nextCompleted;
        bar.style.width = `${percent}%`;
        bar.setAttribute('aria-valuenow', percent);
        bar.textContent = `${percent}%`;

        document.getElementById('completedCount')?.replaceChildren(String(nextCompleted));
        document.getElementById('remainingCount')?.replaceChildren(String(Math.max(required - nextCompleted, 0)));

        const progressInfo = document.getElementById('progressInfo');
        if (progressInfo) {
            progressInfo.innerHTML = `<i class="bi bi-info-circle me-1"></i>${nextCompleted} dari ${required} dokumen wajib telah dipenuhi`;
        }
    }

    async function submitUploadForm(form, submitBtn, feedback, row) {
        const response = await fetch(form.action, {
            method: form.method || 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        let payload = null;
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) payload = await response.json();
        if (!response.ok || (payload && payload.ok === false)) {
            throw new Error(payload?.error || 'Failed to upload the document.');
        }

        markUploadSuccess(row, payload);
        updateProgressIndicators(1);
        docs.showToast('success', 'Upload berhasil', payload?.message || 'Dokumen berhasil diunggah.');
        submitBtn.innerHTML = '<i class="bi bi-check2"></i> Uploaded';

        if (feedback) {
            feedback.classList.remove('text-muted', 'text-danger');
            feedback.classList.add('text-success');
            feedback.innerHTML = '<i class="bi bi-check2-circle me-1"></i>Upload berhasil. Refresh halaman untuk aksi lanjutan.';
        }
    }

    function initUploadFormHandlers() {
        docs.bindOnce('form[data-upload-form="true"]', 'boundUploadForm', form => {
            form.addEventListener('submit', async function (e) {
                e.preventDefault();
                const row = this.dataset.row;
                const fileInput = this.querySelector('.file-input');
                const submitBtn = this.querySelector('.upload-btn');
                const feedback = document.querySelector(`.upload-feedback[data-row="${row}"]`);

                if (!fileInput?.files?.length) {
                    docs.showToast('warning', 'No file selected', 'Please select or drag & drop a document first.');
                    return;
                }

                const originalHtml = submitBtn.innerHTML;
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Uploading...';
                if (feedback) {
                    feedback.classList.remove('text-danger', 'text-success');
                    feedback.classList.add('text-muted');
                    feedback.textContent = 'Mengunggah dokumen...';
                }

                try {
                    await submitUploadForm(this, submitBtn, feedback, row);
                } catch (err) {
                    console.error('Upload error:', err);
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalHtml;
                    if (feedback) {
                        feedback.classList.remove('text-muted', 'text-success');
                        feedback.classList.add('text-danger');
                        feedback.innerHTML = `<i class="bi bi-exclamation-triangle me-1"></i>${err.message || 'Upload gagal.'}`;
                    }
                    docs.showToast('error', 'Upload gagal', err.message || 'Terjadi kesalahan.', 5000);
                }
            });
        });
    }

    function initGenerateFormHandlers() {
        docs.bindOnce('form[data-generate-form="true"]', 'boundGenerateForm', form => {
            const submitBtn = form.querySelector('.js-generate-btn');
            if (!submitBtn) return;
            form.addEventListener('submit', () => {
                if (submitBtn.dataset.loading === 'true') return;
                submitBtn.dataset.loading = 'true';
                submitBtn.dataset.originalHtml = submitBtn.innerHTML;
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Processing...';
            });
        });
    }

    docs.onBoot(() => {
        initUploadFormHandlers();
        initGenerateFormHandlers();
    });
})(window);
