(function (app) {
    app.buildRoundRow = function (ctx, block, preset) {
        const row = document.createElement('div');
        row.className = 'row g-2 align-items-end';
        row.dataset.roundRow = '1';
        row.innerHTML = `
            <div class="col-md-9">
                <label class="form-label">Harga Penawaran</label>
                <div class="input-group input-group-sm">
                    <span class="input-group-text">Rp</span>
                    <input class="form-control text-end" data-price-input data-format-currency value="${preset?.price ?? ''}" inputmode="numeric" />
                </div>
                <span class="text-danger small field-validation-valid" data-valmsg-price data-valmsg-replace="true"></span>
            </div>
            <div class="col-md-3 text-end">
                <button type="button" class="btn btn-outline-danger btn-sm" data-remove-round>
                    <i class="bi bi-trash"></i> Hapus round
                </button>
            </div>`;

        row.querySelector('[data-remove-round]')?.addEventListener('click', () => {
            const host = row.parentElement;
            row.remove();
            if (host && host.querySelectorAll('[data-round-row]').length === 0) {
                host.appendChild(app.buildRoundRow(ctx, block));
            }
            const card = block.closest('[data-vendor-card]');
            app.syncLetterRowsToItemRounds(ctx, card);
            app.reindexVendorCards(ctx);
            app.recalcSummary(ctx);
        });

        const priceInput = row.querySelector('[data-price-input]');
        if (priceInput && preset?.price) {
            app.setCurrencyDisplay(priceInput, preset.price.toString());
        }
        app.bindCurrencyWithin(ctx, row);
        row.querySelectorAll('[data-price-input]').forEach(input => {
            input.addEventListener('input', () => {
                app.reindexVendorCards(ctx);
                app.recalcSummary(ctx);
            });
        });
        return row;
    };

    app.buildVendorItem = function (ctx, meta, preset) {
        const block = document.createElement('div');
        block.className = 'border rounded-3 p-3 bg-body-tertiary';
        block.dataset.vendorItem = '1';
        block.dataset.procOfferId = meta.procOfferId;
        block.dataset.itemName = meta.itemPenawaran;
        const isIncluded = preset?.isIncluded !== false;
        block.dataset.isIncluded = isIncluded ? 'true' : 'false';
        const qtyNumeric = Number(meta.quantity);
        const woQtyDisplay = Number.isFinite(qtyNumeric) ? app.formatInteger(qtyNumeric) : '-';
        const revenueRow = document.querySelector(`#pdcItems [data-item-row][data-proc-offer-id="${meta.procOfferId}"]`);
        const formFields = ctx.isPengangkutan
            ? buildTransportFields(preset, meta)
            : buildUnitFields(preset, meta, revenueRow);

        block.innerHTML = `
            <input type="hidden" value="${meta.procOfferId}" data-proc-offer-input />
            <input type="hidden" data-item-index />
            <input type="hidden" value="${isIncluded ? 'true' : 'false'}" data-is-included-input />
            <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                <div class="d-flex align-items-center gap-2">
                    <div class="form-check">
                        <input type="checkbox" class="form-check-input" data-include-checkbox ${isIncluded ? 'checked' : ''} title="Include item ini dalam penawaran vendor" />
                    </div>
                    <div>
                        <div class="fw-semibold">${meta.itemPenawaran}</div>
                        <div class="small text-muted">WO Qty: ${woQtyDisplay}</div>
                    </div>
                </div>
                <div class="hstack gap-1">
                    <button type="button" class="btn btn-outline-secondary btn-sm" data-toggle-item><i class="bi bi-chevron-up"></i></button>
                    <button type="button" class="btn btn-outline-primary btn-sm" data-add-round><i class="bi bi-plus-circle"></i> Round</button>
                </div>
            </div>
            <div class="mt-3 border rounded-3 p-2 bg-body" data-item-body>
                <div class="row g-3">${formFields}</div>
                <div class="mt-3 vstack gap-2" data-rounds></div>
            </div>`;

        wireVendorItem(ctx, block, preset, isIncluded);
        return block;
    };

    function buildTransportFields(preset, meta) {
        const defaultQty = preset?.quantity || meta.quantity || 1;
        const defaultTrip = preset?.trip || 1;
        return `
            <div class="col-md-3">
                <label class="form-label">Quantity</label>
                <input class="form-control form-control-sm text-end" data-quantity-input value="${defaultQty}" inputmode="numeric" />
                <span class="text-danger small field-validation-valid" data-valmsg-qty data-valmsg-replace="true"></span>
            </div>
            <div class="col-md-3">
                <label class="form-label">Trip</label>
                <input class="form-control form-control-sm text-end" data-trip-input value="${defaultTrip}" inputmode="numeric" />
                <span class="text-danger small field-validation-valid" data-valmsg-trip data-valmsg-replace="true"></span>
            </div>`;
    }

    function buildUnitFields(preset, meta, revenueRow) {
        const qtyItems = revenueRow?.querySelector('[data-qty-items]')?.value || meta.quantity || '';
        const unitItems = revenueRow?.querySelector('[data-unit-items]')?.value || '';
        const unitRevenue = revenueRow?.querySelector('[data-unit-revenue]')?.selectedOptions?.[0]?.text || '';
        return `
            <div class="col-md-2">
                <label class="form-label">Qty Items</label>
                <input class="form-control form-control-sm text-end" value="${qtyItems}" readonly data-qty-items-display />
            </div>
            <div class="col-md-2">
                <label class="form-label">Unit Items</label>
                <input class="form-control form-control-sm text-end" value="${unitItems}" readonly data-unit-items-display />
            </div>
            <div class="col-md-3">
                <label class="form-label">Quantity/Durasi</label>
                <input class="form-control form-control-sm text-end" data-quantity-input value="${preset?.quantity ?? meta.quantity ?? ''}" inputmode="numeric" />
                <span class="text-danger small field-validation-valid" data-valmsg-qty data-valmsg-replace="true"></span>
            </div>
            <div class="col-md-2">
                <label class="form-label">Unit Revenue</label>
                <input class="form-control form-control-sm text-end" value="${unitRevenue}" readonly data-unit-revenue-display />
            </div>
            <input type="hidden" data-trip-input value="${preset?.trip || 1}" />`;
    }

    function wireVendorItem(ctx, block, preset, isIncluded) {
        const roundsHost = block.querySelector('[data-rounds]');
        const presetRounds = Array.isArray(preset?.prices) && preset.prices.length
            ? preset.prices.map(price => ({ price }))
            : [{ price: '' }];
        presetRounds.forEach(round => roundsHost.appendChild(app.buildRoundRow(ctx, block, round)));
        wireIncludedToggle(ctx, block, isIncluded);
        wireItemButtons(ctx, block, roundsHost);
    }

    function wireIncludedToggle(ctx, block, isIncluded) {
        const includeCheckbox = block.querySelector('[data-include-checkbox]');
        const isIncludedInput = block.querySelector('[data-is-included-input]');
        const itemBody = block.querySelector('[data-item-body]');
        includeCheckbox?.addEventListener('change', () => {
            const checked = includeCheckbox.checked;
            block.dataset.isIncluded = checked ? 'true' : 'false';
            if (isIncludedInput) isIncludedInput.value = checked ? 'true' : 'false';
            if (itemBody) {
                itemBody.style.opacity = checked ? '1' : '0.5';
                itemBody.style.pointerEvents = checked ? 'auto' : 'none';
            }
            app.reindexVendorCards(ctx);
            app.recalcSummary(ctx);
        });
        if (!isIncluded && itemBody) {
            itemBody.style.opacity = '0.5';
            itemBody.style.pointerEvents = 'none';
        }
    }

    function wireItemButtons(ctx, block, roundsHost) {
        block.querySelector('[data-add-round]')?.addEventListener('click', () => {
            roundsHost.appendChild(app.buildRoundRow(ctx, block));
            app.syncLetterRowsToItemRounds(ctx, block.closest('[data-vendor-card]'));
            app.reindexVendorCards(ctx);
        });
        const body = block.querySelector('[data-item-body]');
        const toggleBtn = block.querySelector('[data-toggle-item]');
        toggleBtn?.addEventListener('click', () => {
            const collapsed = body?.classList.toggle('d-none');
            toggleBtn.innerHTML = collapsed ? '<i class="bi bi-chevron-down"></i>' : '<i class="bi bi-chevron-up"></i>';
        });
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
