(function (app) {
    app.buildLetterRow = function (ctx, card, preset = {}) {
        const row = document.createElement('div');
        row.className = 'border rounded-3 p-2 bg-body';
        row.dataset.letterRow = '1';
        row.innerHTML = `
            <div class="row g-2 align-items-baseline">
                <div class="col-lg-3 col-md-4">
                    <label class="form-label" data-letter-round-label>Round 1 - No. SPH</label>
                    <input class="form-control form-control-sm" data-letter-number value="${preset?.letter ?? ''}" maxlength="150" />
                    <span class="text-danger small field-validation-valid" data-valmsg-letter data-valmsg-replace="true"></span>
                </div>
                <div class="col-lg-5 col-md-5">
                    <label class="form-label" data-letter-file-label>File SPH (PDF)</label>
                    <input type="file" class="form-control form-control-sm" accept="application/pdf" data-letter-file />
                    <span class="text-danger small field-validation-valid" data-valmsg-letter-file data-valmsg-replace="true"></span>
                    <span class="form-text small text-muted">Unggah PDF maksimal 25 MB.</span>
                    <span class="small text-muted" data-letter-status></span>
                </div>
                <div class="col-lg-4 col-md-3 d-flex flex-column gap-2 align-items-end">
                    <input type="hidden" data-letter-doc-id value="${preset?.docId ?? ''}" />
                    <input type="hidden" data-letter-delete value="false" />
                    <div class="d-flex flex-wrap gap-2">
                        <button type="button" class="btn btn-outline-primary btn-sm d-none" data-letter-preview>
                            <i class="bi bi-eye"></i><span>Preview</span>
                        </button>
                        <button type="button" class="btn btn-outline-danger btn-sm" data-letter-remove>
                            <i class="bi bi-trash"></i><span>Hapus file</span>
                        </button>
                    </div>
                </div>
            </div>`;

        const docId = preset?.docId;
        app.setLetterPreviewSource(row, docId);
        wirePreview(ctx, row);
        wireFileChange(row, docId);
        wireRemove(ctx, card, row);
        if (docId) {
            app.setLetterStatus(row, 'Dokumen tersimpan.', 'success');
        }
        app.updateLetterRowLabel(row, 0);
        return row;
    };

    function wirePreview(ctx, row) {
        const previewBtn = row.querySelector('[data-letter-preview]');
        if (!previewBtn) {
            return;
        }
        app.bind(ctx, previewBtn, 'click', async () => {
            if (previewBtn.disabled || previewBtn.classList.contains('disabled')) {
                return;
            }
            const docId = previewBtn.dataset.docId;
            if (!docId) {
                return;
            }
            previewBtn.disabled = true;
            previewBtn.classList.add('disabled');
            previewBtn.setAttribute('aria-busy', 'true');
            try {
                const response = await fetch(app.buildDocPreviewUrl(ctx, docId), {
                    headers: { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
                    method: 'GET'
                });
                if (!response.ok) {
                    throw new Error('Gagal membuka preview dokumen.');
                }
                const payload = await response.json();
                if (!payload?.ok || !payload?.url) {
                    throw new Error(payload?.error || 'Preview belum tersedia.');
                }
                app.showLetterPreview(ctx, payload.url);
            } catch (err) {
                const message = err?.message || 'Tidak dapat menampilkan dokumen.';
                app.debugError('Preview SPH/SNH gagal', err);
                window.Swal?.fire
                    ? window.Swal.fire({ icon: 'error', title: 'Preview gagal', text: message })
                    : alert(message);
            } finally {
                previewBtn.disabled = false;
                previewBtn.classList.remove('disabled');
                previewBtn.removeAttribute('aria-busy');
            }
        });
    }

    function wireFileChange(row, docId) {
        const fileInput = row.querySelector('[data-letter-file]');
        fileInput?.addEventListener('change', () => {
            const file = fileInput.files?.[0];
            if (file) {
                app.setLetterStatus(row, `File terpilih: ${file.name}`, 'success');
                if (row.dataset.letterDeleted === 'true') {
                    app.toggleLetterRowDeleteState(row, false);
                }
            } else if (!docId) {
                app.setLetterStatus(row, '', 'muted');
            }
        });
    }

    function wireRemove(ctx, card, row) {
        row.querySelector('[data-letter-remove]')?.addEventListener('click', () => {
            const existingDocId = row.querySelector('[data-letter-doc-id]')?.value;
            if (existingDocId) {
                app.toggleLetterRowDeleteState(row, row.dataset.letterDeleted !== 'true');
                return;
            }
            row.remove();
            const host = card?.querySelector('[data-letter-host]');
            if (host && host.querySelectorAll('[data-letter-row]').length === 0) {
                host.appendChild(app.buildLetterRow(ctx, card));
            }
            app.updateLetterEmptyState(card);
            app.reindexVendorCards(ctx);
        });
    }

    app.assignLetterFieldNames = function (card, vendorIdx) {
        const host = card?.querySelector('[data-letter-host]');
        if (!host) {
            return;
        }
        host.querySelectorAll('[data-letter-row]').forEach((row, letterIdx) => {
            const letterInput = row.querySelector('[data-letter-number]');
            const fileInput = row.querySelector('[data-letter-file]');
            const docInput = row.querySelector('[data-letter-doc-id]');
            const deleteInput = row.querySelector('[data-letter-delete]');
            const letterMsg = row.querySelector('[data-valmsg-letter]');
            const fileMsg = row.querySelector('[data-valmsg-letter-file]');
            if (letterInput) letterInput.name = `Vendors[${vendorIdx}].Letters[${letterIdx}]`;
            if (fileInput) fileInput.name = `Vendors[${vendorIdx}].LetterFiles[${letterIdx}]`;
            if (docInput) docInput.name = `Vendors[${vendorIdx}].LetterDocIds[${letterIdx}]`;
            if (deleteInput) deleteInput.name = `Vendors[${vendorIdx}].LetterDeletes[${letterIdx}]`;
            setValMsg(letterMsg, letterInput?.name);
            setValMsg(fileMsg, fileInput?.name);
            app.updateLetterRowLabel(row, letterIdx);
        });
        app.updateLetterEmptyState(card);
    };

    function setValMsg(message, name) {
        if (!message) {
            return;
        }
        message.setAttribute('data-valmsg-for', name ?? '');
        message.dataset.valmsgFor = name ?? '';
    }

    app.populateVendorLetters = function (ctx, card, preset) {
        const host = card?.querySelector('[data-letter-host]');
        if (!host) {
            return;
        }
        host.innerHTML = '';
        const letters = Array.isArray(preset?.letters) ? preset.letters : [];
        const docIds = Array.isArray(preset?.letterDocIds) ? preset.letterDocIds : [];
        const rowCount = Math.max(letters.length, docIds.length, 1);
        for (let idx = 0; idx < rowCount; idx++) {
            host.appendChild(app.buildLetterRow(ctx, card, {
                letter: letters[idx] ?? '',
                docId: docIds[idx] ?? ''
            }));
        }
        app.toggleLetterSection(card, true);
    };
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
