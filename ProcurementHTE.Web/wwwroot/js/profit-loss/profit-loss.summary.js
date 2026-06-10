(function (app) {
    app.computeVendorFinalOffer = function (ctx, card) {
        if (!card) {
            return 0;
        }
        let total = 0;
        card.querySelectorAll('[data-vendor-item]').forEach(block => {
            if (block.dataset.isIncluded === 'false') {
                return;
            }
            let qty = app.numberValue(block.querySelector('[data-quantity-input]'));
            let trip = app.numberValue(block.querySelector('[data-trip-input]'));
            if (qty <= 0) qty = 1;
            if (trip <= 0) trip = 1;

            let finalPrice = Number.POSITIVE_INFINITY;
            block.querySelectorAll('[data-price-input]').forEach(priceInput => {
                const price = app.numberValue(priceInput);
                if (price > 0 && price < finalPrice) {
                    finalPrice = price;
                }
            });
            if (Number.isFinite(finalPrice) && finalPrice > 0) {
                total += finalPrice * qty * trip;
            }
        });
        return total;
    };

    app.recalcSummary = function (ctx) {
        const revenueField = document.getElementById('revenue');
        const revenue = revenueField?.dataset?.rawValue
            ? parseFloat(revenueField.dataset.rawValue)
            : parseFloat(app.normalizeCurrencyValue(revenueField?.value)) || 0;
        const tbody = document.querySelector('#summary tbody');
        if (tbody) {
            tbody.innerHTML = '';
        }

        const snapshot = [];
        ctx.vendorAssignments.forEach((card, vendorId) => {
            const total = app.computeVendorFinalOffer(ctx, card);
            app.setDisplayNumber(card.querySelector('[data-vendor-total]'), total);
            if (!tbody || total <= 0) {
                return;
            }

            const vendorName = ctx.vendorNameMap.get(vendorId) ?? vendorId;
            const profit = revenue - total;
            const pct = revenue > 0 ? (profit / revenue) * 100 : 0;
            snapshot.push({ vendorId, vendorName, total, profit, pct });
            tbody.appendChild(buildSummaryRow(vendorName, total, profit, pct));
        });

        highlightBestSummaryRow(tbody);
        if (snapshot.length) {
            app.debugLog('Interim summary recalculated', {
                revenue,
                vendorCount: snapshot.length,
                rows: snapshot
            });
        } else {
            app.debugLog('Interim summary cleared', { revenue });
        }
    };

    function buildSummaryRow(vendorName, total, profit, pct) {
        const tr = document.createElement('tr');
        tr.dataset.totalRaw = total.toString();
        tr.innerHTML = `
            <td>${vendorName}</td>
            <td class="text-end">${app.formatInteger(total)}</td>
            <td class="text-end">${app.formatInteger(profit)}</td>
            <td class="text-end">${Math.round(pct)}%</td>`;
        return tr;
    }

    function highlightBestSummaryRow(tbody) {
        if (!tbody) {
            return;
        }
        const rows = [...tbody.querySelectorAll('tr')];
        rows.forEach(row => row.classList.remove('table-success'));
        if (!rows.length) {
            return;
        }
        const best = rows.reduce((acc, row) => {
            const accVal = parseFloat(acc.dataset.totalRaw || '0') || 0;
            const rowVal = parseFloat(row.dataset.totalRaw || '0') || 0;
            return rowVal < accVal ? row : acc;
        }, rows[0]);
        best.classList.add('table-success');
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
