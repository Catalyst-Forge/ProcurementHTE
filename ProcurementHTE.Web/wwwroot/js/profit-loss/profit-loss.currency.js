(function (app) {
    app.normalizeCurrencyValue = function (value) {
        if (value === null || value === undefined) {
            return '';
        }
        if (typeof value === 'number') {
            return Number.isFinite(value) ? Math.floor(value).toString() : '';
        }

        let text = value.toString().trim();
        if (!text) {
            return '';
        }

        text = text.replace(/\D/g, '');
        return text ? text.replace(/^0+(?=\d)/, '') || '0' : '';
    };

    app.formatCurrencyDisplay = function (normalized) {
        if (!normalized || normalized === '0') {
            return '';
        }
        return normalized.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    };

    app.formatInteger = function (value) {
        const num = Number(value ?? 0);
        return Number.isFinite(num)
            ? Math.round(num).toLocaleString('id-ID', { maximumFractionDigits: 0 })
            : '0';
    };

    app.numberValue = function (input) {
        if (!input) {
            return 0;
        }
        if (input.dataset?.rawValue) {
            const parsed = parseFloat(input.dataset.rawValue);
            if (Number.isFinite(parsed)) {
                return parsed;
            }
        }
        const normalized = app.normalizeCurrencyValue(input.value);
        if (normalized.length) {
            const parsed = parseFloat(normalized);
            return Number.isFinite(parsed) ? parsed : 0;
        }
        const fallback = parseFloat((input.value ?? '0').toString().replace(',', '.'));
        return Number.isFinite(fallback) ? fallback : 0;
    };

    app.setCurrencyDisplay = function (input, rawValue) {
        if (!input) {
            return;
        }
        const normalized = app.normalizeCurrencyValue(
            rawValue === undefined ? input.value : rawValue
        );
        input.dataset.rawValue = normalized;
        input.value = app.formatCurrencyDisplay(normalized);
    };

    app.updateCurrencyInput = function (ctx, input, notify) {
        if (!input) {
            return;
        }
        const cursorPos = input.selectionStart ?? input.value.length;
        const digitsBefore = input.value.slice(0, cursorPos).replace(/\D/g, '').length;
        app.setCurrencyDisplay(input, input.value);

        if (document.activeElement === input) {
            const formatted = input.value;
            let seen = 0;
            let caret = formatted.length;
            for (let i = 0; i < formatted.length; i++) {
                if (/\d/.test(formatted[i])) {
                    seen += 1;
                    if (seen >= digitsBefore) {
                        caret = i + 1;
                        break;
                    }
                }
            }
            input.setSelectionRange(formatted ? caret : 0, formatted ? caret : 0);
        }

        if (notify) {
            if (input.closest('#pdcItems')) {
                app.recalcHeader(ctx);
            } else if (input.closest('#vendors')) {
                app.recalcSummary(ctx);
            }
        }
    };

    app.bindCurrencyInput = function (input) {
        if (!input || input.dataset.currencyBound === '1') {
            return;
        }
        input.dataset.currencyBound = '1';
        app.setCurrencyDisplay(input, input.value);
    };

    app.bindCurrencyWithin = function (ctx, scope) {
        (scope || document).querySelectorAll(ctx.currencySelector).forEach(app.bindCurrencyInput);
    };

    app.setDisplayNumber = function (element, value) {
        if (!element) {
            return;
        }
        const numeric = Number(value ?? 0);
        const sanitized = Number.isFinite(numeric) ? numeric : 0;
        if (element.dataset) {
            element.dataset.rawValue = sanitized.toString();
        }
        if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {
            element.value = app.formatInteger(sanitized);
        } else {
            element.textContent = app.formatInteger(sanitized);
        }
    };

    app.setKmDisplay = function (input, value) {
        if (!input) {
            return;
        }
        const numeric = Number(value ?? 0);
        const sanitized = Number.isFinite(numeric) ? numeric : 0;
        input.dataset.rawValue = sanitized.toString();
        input.value = app.formatInteger(sanitized);
    };

    app.getKmValue = function (input) {
        if (!input) {
            return 0;
        }
        if (input.dataset?.rawValue) {
            const parsed = parseFloat(input.dataset.rawValue);
            return Number.isFinite(parsed) ? parsed : 0;
        }
        const parsed = parseFloat((input.value ?? '0').toString().replace(',', '.'));
        return Number.isFinite(parsed) ? parsed : 0;
    };
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
