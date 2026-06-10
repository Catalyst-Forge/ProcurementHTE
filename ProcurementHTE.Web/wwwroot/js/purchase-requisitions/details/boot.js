(function (window, document) {
    function boot() {
        window.PrDetails?.boot();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }

    if (!window.prDetailsHtmxHooked) {
        window.prDetailsHtmxHooked = true;
        document.body?.addEventListener('htmx:afterSwap', boot);
    }
})(window, document);
