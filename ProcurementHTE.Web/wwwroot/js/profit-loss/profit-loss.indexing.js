(function (app) {
    app.cssEscape = function (value) {
        return value ? value.replace(/([:./\[\],])/g, '\\$1') : '';
    };

    app.applyModelErrors = function (ctx) {
        const errors = ctx.modelStateErrors;
        if (!errors || Object.keys(errors).length === 0) {
            return;
        }
        Object.entries(errors).forEach(([field, message]) => {
            const selector = `[data-valmsg-for="${app.cssEscape(field)}"]`;
            document.querySelectorAll(selector).forEach(span => {
                if (!span) return;
                span.textContent = message;
                span.classList.toggle('field-validation-error', Boolean(message));
                span.classList.toggle('field-validation-valid', !message);
            });
        });
    };

    app.reindexVendorCards = function (ctx) {
        const cards = Array.from(ctx.vendorsHost.querySelectorAll('[data-vendor-card]'));
        cards.forEach((card, vendorIdx) => {
            const vendorHidden = card.querySelector('[data-vendor-id-input]');
            const vendorId = card.dataset.vendorId || '';
            if (vendorHidden) {
                vendorHidden.name = `Vendors[${vendorIdx}].VendorId`;
                vendorHidden.value = vendorId;
            }

            let includedIdx = 0;
            card.querySelectorAll('[data-vendor-item]').forEach(block => {
                const isIncluded = block.dataset.isIncluded !== 'false';
                if (isIncluded) {
                    assignIncludedItem(block, vendorIdx, includedIdx);
                    includedIdx++;
                } else {
                    clearExcludedItem(block);
                }
            });

            app.assignLetterFieldNames(card, vendorIdx);
        });
        app.applyModelErrors(ctx);
    };

    function assignIncludedItem(block, vendorIdx, itemIdx) {
        const base = `Vendors[${vendorIdx}].Items[${itemIdx}]`;
        setName(block.querySelector('[data-proc-offer-input]'), `${base}.ProcOfferId`);
        setName(block.querySelector('[data-quantity-input]'), `${base}.Quantity`);
        setName(block.querySelector('[data-trip-input]'), `${base}.Trip`);
        setName(block.querySelector('[data-is-included-input]'), `${base}.IsIncluded`);
        setValMsg(block.querySelector('[data-valmsg-qty]'), `${base}.Quantity`);
        setValMsg(block.querySelector('[data-valmsg-trip]'), `${base}.Trip`);

        block.querySelectorAll('[data-round-row]').forEach((row, roundIdx) => {
            const priceName = `${base}.Prices[${roundIdx}]`;
            setName(row.querySelector('[data-price-input]'), priceName);
            setValMsg(row.querySelector('[data-valmsg-price]'), priceName);
        });
    }

    function clearExcludedItem(block) {
        [
            '[data-proc-offer-input]',
            '[data-quantity-input]',
            '[data-trip-input]',
            '[data-is-included-input]'
        ].forEach(selector => block.querySelector(selector)?.removeAttribute('name'));

        [
            '[data-valmsg-qty]',
            '[data-valmsg-trip]',
            '[data-valmsg-price]'
        ].forEach(selector => {
            block.querySelectorAll(selector).forEach(message => {
                message.removeAttribute('data-valmsg-for');
                delete message.dataset.valmsgFor;
            });
        });

        block.querySelectorAll('[data-price-input]').forEach(input => input.removeAttribute('name'));
    }

    function setName(input, name) {
        if (input) {
            input.name = name;
        }
    }

    function setValMsg(message, name) {
        if (!message) {
            return;
        }
        message.setAttribute('data-valmsg-for', name);
        message.dataset.valmsgFor = name;
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
