(function (app) {
    app.populateUnitRevenueDropdowns = function (ctx) {
        if (ctx.isPengangkutan) {
            return;
        }
        const allowedCodes = ctx.jobTypeUnitMapping[ctx.jobTypeName]
            || ['HARI', 'JAM', 'LSP', 'TRIP', 'KALI'];

        document.querySelectorAll('.unit-revenue-dropdown').forEach(dropdown => {
            const currentValue = dropdown.dataset.currentValue || dropdown.value;
            dropdown.innerHTML = '<option value="">-- Pilih --</option>';

            ctx.unitTypes.forEach(unit => {
                if (!allowedCodes.includes(unit.Code)) {
                    return;
                }
                const option = document.createElement('option');
                option.value = unit.Code;
                option.text = unit.Name;
                option.selected = unit.Code === currentValue;
                dropdown.appendChild(option);
            });

            if (dropdown.dataset.listenerAdded !== '1') {
                app.bind(ctx, dropdown, 'change', event => {
                    app.updateVendorItemsUnitRevenue(event);
                    app.syncVendorItemsFromRevenue(ctx);
                });
                dropdown.dataset.listenerAdded = '1';
            }
        });
    };

    app.updateVendorItemsUnitRevenue = function (event) {
        const dropdown = event.target;
        const row = dropdown.closest('[data-item-row]');
        const procOfferId = row?.dataset?.procOfferId;
        if (!procOfferId) {
            return;
        }
        const unitRevenueText = dropdown.selectedOptions?.[0]?.text || '';
        document
            .querySelectorAll(`[data-vendor-item][data-proc-offer-id="${procOfferId}"]`)
            .forEach(vendorItem => {
                const unitRevenueInput = vendorItem.querySelector('[data-unit-revenue-display]');
                if (unitRevenueInput) {
                    unitRevenueInput.value = unitRevenueText;
                }
            });
    };

    app.syncVendorItemsFromRevenue = function (ctx) {
        document.querySelectorAll('#pdcItems [data-item-row]').forEach(row => {
            const procOfferId = row.dataset.procOfferId;
            if (!procOfferId) {
                return;
            }
            const qtyItems = row.querySelector('[data-qty-items]')?.value || '';
            const unitItems = row.querySelector('[data-unit-items]')?.value || '';
            const unitRevenue = row.querySelector('[data-unit-revenue]')?.selectedOptions?.[0]?.text || '';
            const quantityDurasi = row.querySelector('[data-quantity-durasi]')?.value || '';

            document
                .querySelectorAll(`[data-vendor-item][data-proc-offer-id="${procOfferId}"]`)
                .forEach(vendorItem => {
                    setInputValue(vendorItem.querySelector('[data-qty-items-display]'), qtyItems);
                    setInputValue(vendorItem.querySelector('[data-unit-items-display]'), unitItems);
                    setInputValue(vendorItem.querySelector('[data-unit-revenue-display]'), unitRevenue);

                    const qtyDurasiInput = vendorItem.querySelector('[data-quantity-input]');
                    if (qtyDurasiInput && quantityDurasi !== '' && app.numberValue(qtyDurasiInput) <= 0) {
                        qtyDurasiInput.value = quantityDurasi;
                    }
                });
        });
    };

    function setInputValue(input, value) {
        if (input) {
            input.value = value;
        }
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
