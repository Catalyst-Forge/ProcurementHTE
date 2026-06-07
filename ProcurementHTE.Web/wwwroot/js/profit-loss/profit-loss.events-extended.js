(function (app) {
    app.wireHtmxDebug = function (ctx) {
        if (!window.htmx) {
            app.debugWarn('HTMX instance not detected while wiring debug hooks');
            return;
        }

        app.bind(ctx, document.body, 'htmx:beforeRequest', event => {
            if (originForm(event) === ctx.plForm) {
                app.debugLog('HTMX beforeRequest', {
                    path: event.detail?.pathInfo,
                    verb: event.detail?.verb,
                    boosted: event.detail?.boosted,
                    headers: event.detail?.headers
                });
            }
        });

        app.bind(ctx, document.body, 'htmx:afterRequest', event => {
            if (originForm(event) === ctx.plForm) {
                app.debugLog('HTMX afterRequest', {
                    status: event.detail?.xhr?.status,
                    path: event.detail?.pathInfo
                });
            }
        });

        app.bind(ctx, document.body, 'htmx:responseError', event => {
            if (originForm(event) === ctx.plForm) {
                app.debugError('HTMX response error', {
                    status: event.detail?.xhr?.status,
                    path: event.detail?.pathInfo,
                    error: event.detail?.error
                });
            }
        });
    };

    app.wireDistance = function (ctx) {
        if (!ctx.distanceInput) {
            return;
        }
        app.bind(ctx, ctx.distanceInput, 'input', () => app.recalcKmPer25FromDistance(ctx));
        app.bind(ctx, ctx.distanceInput, 'change', () => app.recalcKmPer25FromDistance(ctx));
    };

    app.wireLetterModal = function (ctx) {
        app.bind(ctx, ctx.letterPreviewModal, 'hidden.bs.modal', () => {
            if (ctx.letterPreviewFrame) {
                ctx.letterPreviewFrame.src = 'about:blank';
            }
        });
    };

    function originForm(event) {
        const sourceElement = event.detail?.elt;
        return sourceElement?.closest?.('form');
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
