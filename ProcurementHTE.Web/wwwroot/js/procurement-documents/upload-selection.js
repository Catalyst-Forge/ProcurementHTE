(function (window) {
    const docs = window.ProcurementDocuments;

    function isPdfFile(file) {
        if (!file) return false;
        const fileName = (file.name || '').toLowerCase();
        const contentType = (file.type || '').toLowerCase();
        return contentType === 'application/pdf' || fileName.endsWith('.pdf');
    }

    function resetFileSelection(row) {
        const label = document.querySelector(`.selected-file[data-row="${row}"]`);
        if (label) {
            label.classList.remove('text-success');
            label.classList.add('text-muted');
            label.innerHTML = '<i class="bi bi-info-circle me-1"></i>No file selected yet';
        }

        const uploadBtn = document.querySelector(`.upload-btn[data-row="${row}"]`);
        if (uploadBtn) uploadBtn.disabled = true;
    }

    function handleFileSelection(row, file, input) {
        if (!file) {
            resetFileSelection(row);
            return;
        }

        if (!isPdfFile(file)) {
            docs.showToast('warning', 'Format tidak didukung', 'Hanya file PDF yang diperbolehkan.', 4000);
            input.value = '';
            resetFileSelection(row);
            return;
        }

        if (file.size > docs.maxUploadSize()) {
            docs.showToast('warning', 'File terlalu besar', 'Ukuran maksimal 10MB.', 4000);
            input.value = '';
            resetFileSelection(row);
            return;
        }

        const label = document.querySelector(`.selected-file[data-row="${row}"]`);
        if (label) {
            label.classList.remove('text-muted', 'text-success');
            label.innerHTML = `<i class="bi bi-file-earmark-pdf text-danger me-1"></i>${file.name}`;
        }

        const uploadBtn = document.querySelector(`.upload-btn[data-row="${row}"]`);
        if (uploadBtn) uploadBtn.disabled = false;
    }

    function assignFileToInput(input, file, fallbackFileList) {
        if (!input || !file) return;
        try {
            const dt = new DataTransfer();
            dt.items.add(file);
            input.files = dt.files;
        } catch {
            if (!fallbackFileList) return;
            try {
                input.files = fallbackFileList;
            } catch {
                // Browser blocks assignment in some older engines.
            }
        }
    }

    function initDropzoneHandlers() {
        docs.bindOnce('.upload-dropzone', 'boundDropzone', zone => {
            const row = zone.dataset.row;
            const input = zone.querySelector('.file-input');
            if (!input) return;

            const prevent = evt => {
                evt.preventDefault();
                evt.stopPropagation();
            };

            ['dragenter', 'dragover'].forEach(evt => {
                zone.addEventListener(evt, e => {
                    prevent(e);
                    if (!zone.classList.contains('disabled')) zone.classList.add('drag-over');
                });
            });

            ['dragleave', 'dragend', 'drop'].forEach(evt => {
                zone.addEventListener(evt, e => {
                    prevent(e);
                    zone.classList.remove('drag-over');
                });
            });

            zone.addEventListener('drop', e => {
                if (zone.classList.contains('disabled')) return;
                const files = Array.from(e.dataTransfer?.files || []).filter(Boolean);
                if (!files.length) return;
                assignFileToInput(input, files[0], e.dataTransfer?.files);
                handleFileSelection(row, files[0], input);
            });

            zone.addEventListener('keydown', e => {
                if (zone.classList.contains('disabled')) return;
                if (e.key !== 'Enter' && e.key !== ' ') return;
                e.preventDefault();
                input.click();
            });
        });
    }

    function initFileInputHandlers() {
        docs.bindOnce('.file-input', 'boundFileInput', input => {
            input.addEventListener('change', function () {
                const row = this.dataset.row;
                const file = this.files?.[0];
                file ? handleFileSelection(row, file, this) : resetFileSelection(row);
            });
        });
        initDropzoneHandlers();
    }

    docs.isPdfFile = isPdfFile;
    docs.resetFileSelection = resetFileSelection;
    docs.onBoot(initFileInputHandlers);
})(window);
