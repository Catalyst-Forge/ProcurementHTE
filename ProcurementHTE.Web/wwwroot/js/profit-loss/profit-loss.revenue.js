(function (app) {
    app.recalcHeader = function (ctx) {
        let revenueSum = 0;
        document.querySelectorAll('#pdcItems [data-rev]').forEach(input => {
            revenueSum += app.numberValue(input);
        });

        app.setDisplayNumber(document.getElementById('revenue'), revenueSum);
        app.recalcSummary(ctx);
    };
})(window.PHTEProfitLoss = window.PHTEProfitLoss || {});
