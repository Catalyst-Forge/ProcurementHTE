(function (app) {
    app.recalcHeader = function (ctx, options = {}) {
        const forceDistanceKm = options.forceDistanceKm ?? !ctx.isEditMode;
        let revenueSum = 0;
        const itemSnapshots = [];

        if (ctx.isPengangkutan) {
            revenueSum = recalcTransportRows(ctx, forceDistanceKm, itemSnapshots);
        } else {
            revenueSum = recalcUnitRows(itemSnapshots);
        }

        app.setDisplayNumber(document.getElementById('revenue'), revenueSum);
        app.debugLog('Header totals recalculated', {
            jobType: ctx.jobTypeName,
            revenue: revenueSum,
            items: itemSnapshots
        });
        app.recalcSummary(ctx);
    };

    app.recalcKmPer25FromDistance = function (ctx) {
        if (!ctx.distanceInput) {
            app.recalcHeader(ctx);
            return;
        }
        const distance = parseFloat(ctx.distanceInput.value.toString().replace(',', '.') || '0') || 0;
        const kmValue = distance > 400 ? Math.max(0, (distance - 400) / 25) : 0;
        document.querySelectorAll('[data-km]').forEach(input => app.setKmDisplay(input, kmValue));
        app.recalcHeader(ctx, { forceDistanceKm: true });
    };

    function recalcTransportRows(ctx, forceDistanceKm, snapshots) {
        const distance = parseFloat(ctx.distanceInput?.value?.toString().replace(',', '.') ?? '0') || 0;
        let revenueSum = 0;
        document.querySelectorAll('#pdcItems [data-item-row]').forEach(row => {
            const unitQty = app.numberValue(row.querySelector('[data-unit-qty]'));
            const tarifAwal = app.numberValue(row.querySelector('[data-tarif-awal]'));
            const tarifAdd = app.numberValue(row.querySelector('[data-tarif-add]'));
            const kmInput = row.querySelector('[data-km]');
            let km = 0;
            if (forceDistanceKm && distance > 400) {
                km = Math.max(0, (distance - 400) / 25);
                app.setKmDisplay(kmInput, km);
            } else if (kmInput) {
                km = app.getKmValue(kmInput);
            }
            const operatorCost = tarifAdd * km;
            const revenue = (tarifAwal + operatorCost) * unitQty;
            app.setDisplayNumber(row.querySelector('[data-operator]'), operatorCost);
            app.setDisplayNumber(row.querySelector('[data-rev]'), revenue);
            revenueSum += revenue;
            snapshots.push({ procOfferId: row.dataset.procOfferId, unitQty, tarifAwal, tarifAdd, km, operatorCost, revenue });
        });
        return revenueSum;
    }

    function recalcUnitRows(snapshots) {
        let revenueSum = 0;
        document.querySelectorAll('#pdcItems [data-item-row]').forEach(row => {
            const qtyItems = app.numberValue(row.querySelector('[data-qty-items]'));
            const basePrice = app.numberValue(row.querySelector('[data-base-price]'));
            const quantityDurasi = app.numberValue(row.querySelector('[data-quantity-durasi]'));
            const revenue = basePrice * qtyItems * quantityDurasi;
            app.setDisplayNumber(row.querySelector('[data-rev]'), revenue);
            revenueSum += revenue;
            snapshots.push({ procOfferId: row.dataset.procOfferId, qtyItems, basePrice, quantityDurasi, revenue });
        });
        return revenueSum;
    }
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
