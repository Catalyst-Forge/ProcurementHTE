(function (app) {
    app.init = function (config) {
        const ctx = app.createContext(config || {});
        if (!ctx.plForm || !ctx.vendorsHost) {
            app.debugWarn('ProfitLoss script aborted: missing critical DOM nodes', {
                hasForm: Boolean(ctx.plForm),
                hasVendorsHost: Boolean(ctx.vendorsHost)
            });
            return;
        }

        app.disableJqueryValidation(ctx.plForm);
        app.installInstance(ctx);
        app.debugLog('ProfitLoss script initialized', {
            procurementId: ctx.plForm.querySelector('input[name="ProcurementId"]')?.value,
            vendorChoiceCount: ctx.vendorsAll?.length ?? 0,
            presetVendorCount: ctx.presetVendors?.length ?? 0,
            isEdit: Boolean(ctx.plForm.querySelector('input[name="ProfitLossId"]'))
        });

        app.bindCurrencyWithin(ctx, document);
        app.populateUnitRevenueDropdowns(ctx);
        try {
            app.syncVendorItemsFromRevenue(ctx);
        } catch (error) {
            app.debugWarn('syncVendorItemsFromRevenue init failed', error);
        }

        app.wireEvents(ctx);
        app.initPresetVendors(ctx);
        app.refreshVendorSelectOptions(ctx);
        app.syncToggleButtonLabel(ctx);
        app.syncAddVendorButtonState(ctx);

        if (ctx.isEditMode) {
            app.recalcHeader(ctx, { forceDistanceKm: false });
        } else {
            app.recalcKmPer25FromDistance(ctx);
        }
        app.reindexVendorCards(ctx);
        app.recalcSummary(ctx);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => app.init(window.PHTEProfitLossConfig));
    } else {
        app.init(window.PHTEProfitLossConfig);
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
