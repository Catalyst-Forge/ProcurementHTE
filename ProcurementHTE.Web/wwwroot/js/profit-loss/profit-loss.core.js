(function (app) {
    app.createContext = function (config) {
        const vendorsAll = config.vendorsAll || [];
        const presetVendors = config.presetVendors || [];
        const ctx = {
            config,
            vendorsAll,
            procItems: config.procItems || [],
            presetVendors,
            modelStateErrors: config.modelStateErrors || {},
            unitTypes: config.unitTypes || [],
            jobTypeName: config.jobTypeName || 'Angkutan',
            isEditMode: Boolean(config.isEditMode),
            docPreviewUrlTemplate: config.docPreviewUrlTemplate || '#',
            vendorCheckboxSelector: 'input[name="SelectedVendorIds"]',
            currencySelector: '[data-format-currency]',
            vendorNameMap: new Map(vendorsAll.map(vendor => [vendor.id, vendor.name])),
            vendorsHost: document.getElementById('vendors'),
            addVendorBtn: document.getElementById('btnAddVendor'),
            plForm: document.getElementById('pl-form'),
            distanceInput: document.getElementById('Distance'),
            letterPreviewModal: document.getElementById('letterPreviewModal'),
            letterPreviewFrame: document.getElementById('letterPreviewFrame'),
            bootstrapModal: window.bootstrap?.Modal,
            forcedSubmitInProgress: false,
            cleanup: [],
            instanceKey: '__plCreateEditInstance',
            vendorCards: new Set(),
            vendorAssignments: new Map(),
            presetVendorMap: new Map(presetVendors.map(vendor => [vendor.vendorId, vendor])),
            jobTypeUnitMapping: {
                Angkutan: ['TRIP'],
                StandBy: ['HARI', 'JAM', 'LSP'],
                Moving: ['TRIP', 'KALI']
            }
        };

        ctx.isPengangkutan = ctx.jobTypeName === 'Angkutan';
        ctx.isSewaUnit = ctx.jobTypeName === 'StandBy';
        ctx.isMoving = ctx.jobTypeName === 'Moving';
        return ctx;
    };

    app.bind = function (ctx, target, eventName, handler, options) {
        if (!target?.addEventListener || typeof handler !== 'function') {
            return;
        }
        target.addEventListener(eventName, handler, options);
        ctx.cleanup.push(() => {
            try {
                target.removeEventListener(eventName, handler, options);
            } catch {
                // ignore stale DOM cleanup
            }
        });
    };

    app.installInstance = function (ctx) {
        const previousInstance = window[ctx.instanceKey];
        previousInstance?.dispose?.();

        const instance = {
            dispose() {
                while (ctx.cleanup.length) {
                    const dispose = ctx.cleanup.pop();
                    try {
                        dispose?.();
                    } catch {
                        // ignore stale DOM cleanup
                    }
                }
                if (window[ctx.instanceKey] === instance) {
                    delete window[ctx.instanceKey];
                }
            }
        };
        window[ctx.instanceKey] = instance;
        ctx.instance = instance;

        app.bind(ctx, document.body, 'htmx:beforeSwap', event => {
            const target = event.detail?.target;
            if (target && (target === ctx.plForm || target.contains(ctx.plForm))) {
                instance.dispose();
            }
        });
    };

    app.debugLog = function (...args) {
        if (window?.console?.info) {
            console.info('[PnL]', ...args);
        }
    };

    app.debugWarn = function (...args) {
        if (window?.console?.warn) {
            console.warn('[PnL]', ...args);
        }
    };

    app.debugError = function (...args) {
        if (window?.console?.error) {
            console.error('[PnL]', ...args);
        }
    };

    app.enqueueMicrotask = typeof window.queueMicrotask === 'function'
        ? window.queueMicrotask.bind(window)
        : fn => Promise.resolve().then(fn);

    app.disableJqueryValidation = function (form) {
        if (!window.jQuery || !jQuery.validator || !form) {
            return;
        }
        const $form = jQuery(form);
        try {
            $form.removeData('validator');
            $form.removeData('unobtrusiveValidation');
            $form.off('.validate');
            $form.find(':input').off('.validate');
        } catch {
            // validation cleanup is best-effort only
        }
    };

    app.showLetterPreview = function (ctx, url) {
        if (!url) {
            return;
        }
        if (!ctx.letterPreviewModal || !ctx.letterPreviewFrame || !ctx.bootstrapModal) {
            const win = window.open(url, '_blank', 'noopener');
            if (win) {
                win.opener = null;
            }
            return;
        }
        ctx.letterPreviewFrame.src = url;
        ctx.bootstrapModal.getOrCreateInstance(ctx.letterPreviewModal).show();
    };
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
