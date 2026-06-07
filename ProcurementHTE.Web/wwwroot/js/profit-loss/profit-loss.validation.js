(function (app) {
    app.validateVendorCoverage = function (ctx) {
        const errors = [];
        const participatingVendors = Array.from(ctx.vendorAssignments.keys());

        ctx.vendorCards.forEach(card => {
            if (!card.dataset.vendorId) {
                errors.push('Ada form Vendor Offer tanpa vendor terpilih. Mohon pilih vendor atau hapus form tersebut.');
            }
        });

        if (!participatingVendors.length) {
            errors.push('Minimal satu vendor harus mengirim penawaran lengkap.');
        }

        ctx.vendorAssignments.forEach((card, vendorId) => {
            const vendorName = ctx.vendorNameMap.get(vendorId) ?? vendorId;
            card.querySelectorAll('[data-vendor-item]').forEach(block => {
                if (block.dataset.isIncluded === 'false') {
                    return;
                }
                const itemName = block.dataset.itemName ?? block.dataset.procOfferId ?? 'Item';
                const qty = app.numberValue(block.querySelector('[data-quantity-input]'));
                const trip = app.numberValue(block.querySelector('[data-trip-input]'));
                const hasPrice = [...block.querySelectorAll('[data-price-input]')]
                    .some(input => app.numberValue(input) > 0);
                if (qty <= 0 || trip <= 0 || !hasPrice) {
                    errors.push(`${vendorName} - ${itemName}: lengkapi quantity, trip, dan minimal satu harga.`);
                }
            });
        });

        if (!errors.length) {
            return true;
        }

        app.debugWarn('Vendor coverage blocked submission', { errors, participatingVendors });
        if (window.Swal?.fire) {
            window.Swal.fire({
                icon: 'warning',
                title: 'Lengkapi Penawaran Vendor',
                html: errors.map(error => `- ${error}`).join('<br/>')
            });
        } else {
            alert('Lengkapi penawaran vendor berikut:\n- ' + errors.join('\n- '));
        }
        return false;
    };
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
