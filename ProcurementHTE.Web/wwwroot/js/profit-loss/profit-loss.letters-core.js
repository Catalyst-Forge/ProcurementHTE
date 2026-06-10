(function (app) {
    app.buildDocPreviewUrl = function (ctx, docId) {
        return docId
            ? ctx.docPreviewUrlTemplate.replace('__DOC_ID__', encodeURIComponent(docId))
            : '#';
    };

    app.updateLetterRowLabel = function (row, letterIdx) {
        if (!row) {
            return;
        }
        const roundNumber = letterIdx + 1;
        const letterType = roundNumber === 1 ? 'SPH' : 'SNH';
        const label = row.querySelector('[data-letter-round-label]');
        const fileLabel = row.querySelector('[data-letter-file-label]');
        const input = row.querySelector('[data-letter-number]');
        if (label) {
            label.textContent = `Round ${roundNumber} - No. ${letterType}`;
        }
        if (fileLabel) {
            fileLabel.textContent = `File ${letterType} (PDF)`;
        }
        if (input) {
            input.placeholder = `Contoh: ${letterType}-001`;
        }
    };

    app.setLetterStatus = function (row, message, tone = 'muted') {
        const status = row?.querySelector('[data-letter-status]');
        if (!status) {
            return;
        }
        status.textContent = message || '';
        status.classList.toggle('text-muted', tone === 'muted');
        status.classList.toggle('text-success', tone === 'success');
        status.classList.toggle('text-danger', tone === 'danger');
    };

    app.setLetterPreviewSource = function (row, docId) {
        const previewBtn = row?.querySelector('[data-letter-preview]');
        if (!previewBtn) {
            return;
        }
        previewBtn.dataset.docId = docId ?? '';
        previewBtn.classList.toggle('d-none', !docId);
        previewBtn.disabled = !docId;
        previewBtn.classList.toggle('disabled', !docId);
    };

    app.toggleLetterRowDeleteState = function (row, deleted) {
        if (!row) {
            return;
        }
        row.dataset.letterDeleted = deleted ? 'true' : 'false';
        const deleteInput = row.querySelector('[data-letter-delete]');
        if (deleteInput) {
            deleteInput.value = deleted ? 'true' : 'false';
        }

        const fileInput = row.querySelector('[data-letter-file]');
        if (fileInput) {
            fileInput.toggleAttribute('disabled', deleted);
            if (deleted) {
                fileInput.value = '';
            }
        }

        row.classList.toggle('border-danger-subtle', deleted);
        row.classList.toggle('bg-danger-subtle', deleted);
        const previewBtn = row.querySelector('[data-letter-preview]');
        if (previewBtn) {
            previewBtn.disabled = deleted;
            previewBtn.classList.toggle('disabled', deleted);
        }

        if (deleted) {
            app.setLetterStatus(row, 'Dokumen akan dihapus saat disimpan.', 'danger');
            return;
        }

        const docIdValue = row.querySelector('[data-letter-doc-id]')?.value;
        app.setLetterStatus(row, docIdValue ? 'Dokumen tersimpan.' : '', docIdValue ? 'success' : 'muted');
    };

    app.updateLetterEmptyState = function (card) {
        const section = card?.querySelector('[data-letter-section]');
        const host = section?.querySelector('[data-letter-host]');
        const alert = section?.querySelector('[data-letter-empty-state]');
        if (!section || !host || !alert) {
            return;
        }
        alert.classList.toggle('d-none', Boolean(host.querySelector('[data-letter-row]')));
    };

    app.toggleLetterSection = function (card, show) {
        const section = card?.querySelector('[data-letter-section]');
        const host = section?.querySelector('[data-letter-host]');
        if (!section || !host) {
            return;
        }
        if (!show) {
            host.innerHTML = '';
            section.classList.add('d-none');
        } else {
            section.classList.remove('d-none');
        }
        app.updateLetterEmptyState(card);
    };

    app.syncLetterRowsToItemRounds = function (ctx, card) {
        const host = card?.querySelector('[data-letter-host]');
        if (!host) {
            return;
        }
        const roundCounts = Array
            .from(card.querySelectorAll('[data-vendor-item]'))
            .map(block => block.querySelectorAll('[data-round-row]').length);
        const maxRounds = Math.max(1, ...(roundCounts.length ? roundCounts : [0]));
        while (host.querySelectorAll('[data-letter-row]').length < maxRounds) {
            host.appendChild(app.buildLetterRow(ctx, card));
        }
        app.updateLetterEmptyState(card);
    };
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
