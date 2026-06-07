(function (window, document) {
    function boot() {
        window.ProcurementDetails?.boot();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }

    if (!window.procurementDetailsHtmxHooked) {
        window.procurementDetailsHtmxHooked = true;
        document.body?.addEventListener('htmx:afterSwap', boot);
    }
})(window, document);
