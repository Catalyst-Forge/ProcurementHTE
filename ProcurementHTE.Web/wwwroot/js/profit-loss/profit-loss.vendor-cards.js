(function (app) {
    app.resetVendorCardDisplay = function (ctx, card) {
        if (!card) {
            return;
        }
        card.dataset.vendorId = '';
        const hidden = card.querySelector('[data-vendor-id-input]');
        if (hidden) {
            hidden.value = '';
        }
        const itemsHost = card.querySelector('[data-items-host]');
        if (itemsHost) {
            itemsHost.innerHTML = '';
            itemsHost.classList.add('d-none');
        }
        card.querySelector('[data-empty-state]')?.classList.remove('d-none');
        card.querySelector('[data-total-row]')?.classList.add('d-none');
        app.toggleLetterSection(card, false);
        app.setDisplayNumber(card.querySelector('[data-vendor-total]'), 0);
    };

    app.populateVendorCardItems = function (ctx, card, preset) {
        const itemsHost = card?.querySelector('[data-items-host]');
        if (!itemsHost) {
            return;
        }
        itemsHost.innerHTML = '';
        const hasPresetItems = preset?.items && preset.items.length > 0;
        (ctx.procItems || []).forEach(item => {
            const presetItem = preset?.items?.find(it => it.procOfferId === item.procOfferId);
            const effectivePreset = presetItem ?? (hasPresetItems ? { isIncluded: true } : undefined);
            itemsHost.appendChild(app.buildVendorItem(ctx, item, effectivePreset));
        });
        card.querySelector('[data-empty-state]')?.classList.add('d-none');
        itemsHost.classList.remove('d-none');
        card.querySelector('[data-total-row]')?.classList.remove('d-none');
        app.bindCurrencyWithin(ctx, itemsHost);
        app.setVendorItemsCollapsed(ctx, false);
        try {
            app.syncVendorItemsFromRevenue(ctx);
        } catch (error) {
            app.debugWarn('syncVendorItemsFromRevenue failed', error);
        }
    };

    app.assignVendorToCard = function (ctx, card, vendorId, preset) {
        if (!card) {
            return;
        }
        const select = card.querySelector('[data-vendor-select]');
        const previousVendor = card.dataset.vendorId || '';
        if (vendorId === previousVendor) {
            return;
        }
        if (vendorId && ctx.vendorAssignments.has(vendorId) && ctx.vendorAssignments.get(vendorId) !== card) {
            notifyDuplicateVendor();
            if (select) select.value = previousVendor || '';
            return;
        }
        if (previousVendor) {
            ctx.vendorAssignments.delete(previousVendor);
        }
        if (!vendorId) {
            app.resetVendorCardDisplay(ctx, card);
            syncVendorUi(ctx);
            return;
        }

        ctx.vendorAssignments.set(vendorId, card);
        card.dataset.vendorId = vendorId;
        if (select && select.value !== vendorId) select.value = vendorId;
        const hidden = card.querySelector('[data-vendor-id-input]');
        if (hidden) hidden.value = vendorId;

        const vendorPreset = preset ?? ctx.presetVendorMap.get(vendorId);
        app.populateVendorCardItems(ctx, card, vendorPreset);
        app.populateVendorLetters(ctx, card, vendorPreset);
        app.syncLetterRowsToItemRounds(ctx, card);
        syncVendorUi(ctx);
    };

    app.removeVendorCard = function (ctx, card, options = {}) {
        if (!card) {
            return;
        }
        const vendorId = card.dataset.vendorId;
        if (vendorId) {
            ctx.vendorAssignments.delete(vendorId);
            if (options.uncheckVendor !== false) {
                const checkbox = document.getElementById(`vendor_${vendorId}`);
                if (checkbox) checkbox.checked = false;
            }
        }
        ctx.vendorCards.delete(card);
        card.remove();
        syncVendorUi(ctx);
    };

    app.createVendorCard = function (ctx, options = {}) {
        const card = document.createElement('div');
        card.className = 'card border border-primary-subtle';
        card.dataset.vendorCard = '1';
        card.innerHTML = `
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
                <div class="flex-grow-1">
                    <label class="form-label small text-uppercase mb-1 fw-semibold">Vendor Penawaran</label>
                    <select class="form-select form-select-sm" data-vendor-select><option value="">-- Pilih Vendor --</option></select>
                    <div class="small text-muted">Hanya vendor yang tercentang yang akan muncul di daftar.</div>
                </div>
                <button type="button" class="btn btn-outline-danger btn-sm" data-remove-vendor><i class="bi bi-trash"></i> Hapus</button>
            </div>
            <div class="card-body vstack gap-3">
                <input type="hidden" data-vendor-index />
                <input type="hidden" data-vendor-id-input />
                <div class="alert alert-info small mb-0" data-empty-state>Pilih vendor untuk menampilkan detail penawaran.</div>
                <div class="border rounded-3 p-3 bg-body-tertiary d-none" data-letter-section>
                    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                        <div>
                            <div class="fw-semibold">Dokumen SPH / SNH</div>
                            <div class="small text-muted">Round 1 gunakan SPH (PDF), round berikutnya SNH.</div>
                        </div>
                        <button type="button" class="btn btn-outline-primary btn-sm" data-add-letter-round><i class="bi bi-plus-circle"></i> Tambah Round</button>
                    </div>
                    <div class="mt-3 vstack gap-2" data-letter-host></div>
                    <div class="alert alert-warning small mb-0 d-none" data-letter-empty-state>Tambahkan minimal satu nomor dan file SPH/SNH.</div>
                </div>
                <div class="vstack gap-3 d-none" data-items-host></div>
                <div class="d-flex justify-content-end gap-3 align-items-center border-top pt-2 d-none" data-total-row>
                    <div class="fw-semibold">Final Offer</div><span class="fw-bold" data-vendor-total>0</span>
                </div>
            </div>`;

        ctx.vendorsHost.appendChild(card);
        ctx.vendorCards.add(card);
        app.resetVendorCardDisplay(ctx, card);
        wireVendorCard(ctx, card);
        app.refreshVendorSelectOptions(ctx);
        app.syncAddVendorButtonState(ctx);
        if (options.vendorId) {
            app.assignVendorToCard(ctx, card, options.vendorId, options.preset ?? ctx.presetVendorMap.get(options.vendorId));
        }
        return card;
    };

    function wireVendorCard(ctx, card) {
        const vendorSelect = card.querySelector('[data-vendor-select]');
        app.bind(ctx, vendorSelect, 'change', () => {
            app.assignVendorToCard(ctx, card, vendorSelect.value || '', ctx.presetVendorMap.get(vendorSelect.value));
        });
        card.querySelector('[data-remove-vendor]')?.addEventListener('click', () => app.removeVendorCard(ctx, card, { uncheckVendor: false }));
        card.querySelector('[data-add-letter-round]')?.addEventListener('click', () => {
            const host = card.querySelector('[data-letter-host]');
            if (!host) return;
            host.appendChild(app.buildLetterRow(ctx, card));
            app.updateLetterEmptyState(card);
            app.reindexVendorCards(ctx);
        });
    }

    function syncVendorUi(ctx) {
        app.refreshVendorSelectOptions(ctx);
        app.syncAddVendorButtonState(ctx);
        app.reindexVendorCards(ctx);
        app.recalcSummary(ctx);
    }

    function notifyDuplicateVendor() {
        window.Swal?.fire
            ? window.Swal.fire({ icon: 'info', title: 'Vendor sudah digunakan', text: 'Vendor tersebut sudah memiliki form penawaran.' })
            : alert('Vendor tersebut sudah memiliki form penawaran.');
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
