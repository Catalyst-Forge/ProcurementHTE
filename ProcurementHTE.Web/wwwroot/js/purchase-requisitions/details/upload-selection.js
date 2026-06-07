(function (window) {
    const details = window.PrDetails;

    function isPdfFile(file) {
        if (!file) return false;
        const contentType = (file.type || '').toLowerCase();
        const fileName = (file.name || '').toLowerCase();
        return contentType === 'application/pdf' || fileName.endsWith('.pdf');
    }

    function resetFileSelection(row) {
        const label = document.querySelector(`.selected-file[data-row="${row}"]`);
        if (label) {
            label.classList.remove('text-success');
            label.classList.add('text-muted');
            label.innerHTML = '<i class="bi bi-info-circle me-1"></i>No file';
        }
        const button = document.querySelector(`.upload-btn[data-row="${row}"]`);
        if (button) button.disabled = true;
    }

    function handleFileSelection(row, file, input) {
        if (!file) return resetFileSelection(row);
        if (!isPdfFile(file)) {
            details.showToast('warning', 'Only PDF allowed');
            input.value = '';
            return resetFileSelection(row);
        }
        if (file.size > details.maxUploadSize()) {
            details.showToast('warning', 'Max 10MB');
            input.value = '';
            return resetFileSelection(row);
        }

        const label = document.querySelector(`.selected-file[data-row="${row}"]`);
        if (label) {
            label.classList.remove('text-muted', 'text-success');
            label.innerHTML = `<i class="bi bi-file-earmark-pdf text-danger me-1"></i>${file.name}`;
        }
        const button = document.querySelector(`.upload-btn[data-row="${row}"]`);
        if (button) button.disabled = false;
    }

    function assignFileToInput(input, file, fallback) {
        if (!input || !file) return;
        try {
            const transfer = new DataTransfer();
            transfer.items.add(file);
            input.files = transfer.files;
        } catch {
            if (!fallback) return;
            try {
                input.files = fallback;
            } catch {
                // Browser may block assignment.
            }
        }
    }

    function initDropzoneHandlers() {
        details.bindOnce('.upload-dropzone', 'boundDropzone', zone => {
            const row = zone.dataset.row;
            const input = zone.querySelector('.file-input');
            if (!input) return;

            const prevent = event => {
                event.preventDefault();
                event.stopPropagation();
            };

            ['dragenter', 'dragover'].forEach(type => zone.addEventListener(type, event => {
                prevent(event);
                if (!zone.classList.contains('disabled')) zone.classList.add('drag-over');
            }));

            ['dragleave', 'dragend', 'drop'].forEach(type => zone.addEventListener(type, event => {
                prevent(event);
                zone.classList.remove('drag-over');
            }));

            zone.addEventListener('drop', event => {
                if (zone.classList.contains('disabled')) return;
                const files = Array.from(event.dataTransfer?.files || []).filter(Boolean);
                if (!files.length) return;
                assignFileToInput(input, files[0], event.dataTransfer?.files);
                handleFileSelection(row, files[0], input);
            });

            zone.addEventListener('keydown', event => {
                if (zone.classList.contains('disabled')) return;
                if (event.key !== 'Enter' && event.key !== ' ') return;
                event.preventDefault();
                input.click();
            });
        });
    }

    function initFileInputHandlers() {
        details.bindOnce('.file-input', 'boundFileInput', input => {
            input.addEventListener('change', function () {
                const file = this.files?.[0];
                file ? handleFileSelection(this.dataset.row, file, this) : resetFileSelection(this.dataset.row);
            });
        });
        initDropzoneHandlers();
    }

    details.onBoot(initFileInputHandlers);
})(window);
