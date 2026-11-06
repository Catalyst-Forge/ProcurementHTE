(function (app) {
    app.wireEvents = function (ctx) {
        wireToggleButton(ctx);
        wireVendorLayoutButtons(ctx);
        wireVendorCheckboxes(ctx);
        wireVendorSearch(ctx);
        wireAddVendor(ctx);
        wireSubmit(ctx);
        app.wireHtmxDebug(ctx);
        wireDocumentInputs(ctx);
        app.wireLetterModal(ctx);
    };

    app.syncToggleButtonLabel = function (ctx) {
        const toggleVendorBtn = document.getElementById('btnToggleVendors');
        if (!toggleVendorBtn) {
            return;
        }
        const checkboxes = app.getVisibleVendorCheckboxes(ctx);
        const allChecked = checkboxes.length > 0 && checkboxes.every(checkbox => checkbox.checked);
        const labelSpan = toggleVendorBtn.querySelector('span[data-label-checked]');
        if (labelSpan) {
            labelSpan.textContent = allChecked
                ? labelSpan.getAttribute('data-label-checked') ?? 'Uncheck All'
                : labelSpan.getAttribute('data-label-unchecked') ?? 'Check All';
        }
        const icon = toggleVendorBtn.querySelector('i');
        if (icon) {
            icon.className = allChecked ? 'bi bi-square me-1' : 'bi bi-check2-square me-1';
        }
    };

    app.initPresetVendors = function (ctx) {
        if (!Array.isArray(ctx.presetVendors)) {
            return;
        }
        ctx.presetVendors.forEach(vendor => {
            if (!vendor?.vendorId) {
                return;
            }
            const checkbox = document.getElementById(`vendor_${vendor.vendorId}`);
            if (checkbox) {
                checkbox.checked = true;
            }
            app.createVendorCard(ctx, { vendorId: vendor.vendorId, preset: vendor });
        });
    };

    function wireToggleButton(ctx) {
        document.getElementById('btnToggleVendors')?.addEventListener('click', () => {
            const checkboxes = app.getVisibleVendorCheckboxes(ctx);
            const shouldCheckAll = !(checkboxes.length > 0 && checkboxes.every(checkbox => checkbox.checked));
            checkboxes.forEach(checkbox => {
                checkbox.checked = shouldCheckAll;
                checkbox.dispatchEvent(new Event('change'));
            });
            app.syncToggleButtonLabel(ctx);
            app.syncVendorSelectedCount(ctx);
            app.syncAddVendorButtonState(ctx);
        });
    }

    function wireVendorSearch(ctx) {
        const search = document.getElementById('vendorSearch');
        app.bind(ctx, search, 'input', () => app.filterVendorChoices(ctx, search.value));
        app.bind(ctx, search, 'keydown', event => {
            if (event.key === 'Enter') {
                event.preventDefault();
            }
        });
    }

    function wireVendorLayoutButtons(ctx) {
        document.getElementById('btnExpandVendors')?.addEventListener('click', () => app.setVendorItemsCollapsed(ctx, false));
        document.getElementById('btnCollapseVendors')?.addEventListener('click', () => app.setVendorItemsCollapsed(ctx, true));
    }

    function wireVendorCheckboxes(ctx) {
        document.querySelectorAll(ctx.vendorCheckboxSelector).forEach(checkbox => {
            checkbox.addEventListener('change', () => {
                if (!checkbox.checked) {
                    const card = ctx.vendorAssignments.get(checkbox.value);
                    if (card) {
                        app.removeVendorCard(ctx, card, { uncheckVendor: false });
                    }
                }
                app.syncToggleButtonLabel(ctx);
                app.syncVendorSelectedCount(ctx);
                app.refreshVendorSelectOptions(ctx);
                app.syncAddVendorButtonState(ctx);
            });
        });
    }

    function wireAddVendor(ctx) {
        ctx.addVendorBtn?.addEventListener('click', () => {
            if (ctx.addVendorBtn.disabled || !app.getAddableVendorIds(ctx).length) {
                app.syncAddVendorButtonState(ctx);
                return;
            }
            const card = app.createVendorCard(ctx);
            card.querySelector('[data-vendor-select]')?.focus();
            card.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    function wireSubmit(ctx) {
        app.bind(ctx, ctx.plForm, 'submit', event => {
            if (ctx.forcedSubmitInProgress) {
                app.debugLog('Skipping instrumentation for forced submission replay');
                return;
            }
            app.debugLog('Form submit triggered', {
                action: ctx.plForm.action,
                method: ctx.plForm.method,
                selectedVendorCount: app.getSelectedVendorIds(ctx).length,
                vendorCards: ctx.vendorCards.size,
                defaultPrevented: event.defaultPrevented
            });
            if (!app.validateVendorCoverage(ctx)) {
                event.preventDefault();
                event.stopPropagation();
                return;
            }
            normalizeCurrencyBeforeSubmit(ctx);
            forceSubmitIfPrevented(ctx, event);
        });
    }

    function normalizeCurrencyBeforeSubmit(ctx) {
        document.querySelectorAll(ctx.currencySelector).forEach(input => {
            const normalized = input.dataset?.rawValue ?? app.normalizeCurrencyValue(input.value);
            input.dataset.rawValue = normalized;
            input.value = normalized ? normalized.replace('.', ',') : '';
        });
        app.debugLog('Currency fields normalized before submit');
    }

    function forceSubmitIfPrevented(ctx, event) {
        app.enqueueMicrotask(() => {
            if (!event.defaultPrevented || !ctx.plForm.checkValidity()) {
                return;
            }
            app.debugWarn('Submit was prevented by another handler. Forcing manual submit replay.');
            ctx.forcedSubmitInProgress = true;
            try {
                ctx.plForm.submit();
            } finally {
                ctx.forcedSubmitInProgress = false;
            }
        });
    }

    function wireDocumentInputs(ctx) {
        app.bind(ctx, document, 'input', event => {
            const target = event.target;
            if (!target) return;
            if (target.matches(ctx.currencySelector)) {
                app.updateCurrencyInput(ctx, target, true);
            } else if (target.matches('[data-unit-qty],[data-quantity-durasi]')) {
                app.recalcHeader(ctx);
            } else if (target.matches('[data-quantity-input],[data-trip-input]')) {
                app.recalcSummary(ctx);
            }
        });
        app.bind(ctx, document, 'blur', event => {
            if (event.target?.matches(ctx.currencySelector)) {
                app.updateCurrencyInput(ctx, event.target);
            }
        }, true);
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
