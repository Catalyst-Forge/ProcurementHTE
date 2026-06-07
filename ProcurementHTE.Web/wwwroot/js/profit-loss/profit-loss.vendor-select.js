(function (app) {
    app.getSelectedVendorIds = function (ctx) {
        return Array
            .from(document.querySelectorAll(`${ctx.vendorCheckboxSelector}:checked`))
            .map(checkbox => checkbox.value);
    };

    app.getAssignedVendorIds = function (ctx) {
        return Array.from(ctx.vendorAssignments.keys());
    };

    app.getAddableVendorIds = function (ctx) {
        const selected = new Set(app.getSelectedVendorIds(ctx));
        app.getAssignedVendorIds(ctx).forEach(id => selected.delete(id));
        return Array.from(selected);
    };

    app.hasUnassignedCard = function (ctx) {
        return Array.from(ctx.vendorCards).some(card => !card.dataset.vendorId);
    };

    app.syncAddVendorButtonState = function (ctx) {
        if (!ctx.addVendorBtn) {
            return;
        }
        const selectedCount = app.getSelectedVendorIds(ctx).length;
        const available = app.getAddableVendorIds(ctx);
        ctx.addVendorBtn.disabled = selectedCount === 0
            || available.length === 0
            || app.hasUnassignedCard(ctx);
    };

    app.refreshVendorSelectOptions = function (ctx) {
        const selected = app.getSelectedVendorIds(ctx);
        ctx.vendorCards.forEach(card => {
            const select = card.querySelector('[data-vendor-select]');
            if (!select) {
                return;
            }
            const current = card.dataset.vendorId || '';
            select.innerHTML = '';
            const placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = selected.length ? '-- Pilih Vendor --' : 'Tidak ada vendor terpilih';
            placeholder.disabled = selected.length === 0;
            placeholder.selected = !current;
            select.appendChild(placeholder);

            selected.forEach(id => {
                const option = document.createElement('option');
                option.value = id;
                option.textContent = ctx.vendorNameMap.get(id) ?? id;
                option.disabled = ctx.vendorAssignments.has(id) && card.dataset.vendorId !== id;
                option.selected = current === id;
                if (option.selected) {
                    placeholder.selected = false;
                }
                select.appendChild(option);
            });
        });
    };

    app.setVendorItemsCollapsed = function (ctx, collapsed) {
        ctx.vendorCards.forEach(card => {
            card.querySelectorAll('[data-vendor-item]').forEach(block => {
                const body = block.querySelector('[data-item-body]');
                const toggleBtn = block.querySelector('[data-toggle-item]');
                if (!body || !toggleBtn) {
                    return;
                }
                body.classList.toggle('d-none', collapsed);
                toggleBtn.innerHTML = collapsed
                    ? '<i class="bi bi-chevron-down"></i>'
                    : '<i class="bi bi-chevron-up"></i>';
            });
        });
    };
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
