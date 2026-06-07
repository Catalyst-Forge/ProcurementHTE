(function (window) {
    const docs = window.ProcurementDocuments;

    function boot() {
        docs.boot();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }

    if (!window.procDocsHtmxHooked) {
        window.procDocsHtmxHooked = true;
        document.body?.addEventListener('htmx:afterSwap', boot);
    }
})(window);
