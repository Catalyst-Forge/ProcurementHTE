(function (window) {
    const details = window.ProcurementDetails = window.ProcurementDetails || {};

    details.state = details.state || {
        currentDownloadDocId: null,
        currentPreviewUrl: null
    };
    details.bootHandlers = details.bootHandlers || [];

    details.config = function () {
        return window.procurementDetailsConfig || {};
    };

    details.previewUrl = function (docId) {
        const template = details.config().previewUrlTemplate || '#';
        return docId ? template.replace('__DOC_ID__', encodeURIComponent(docId)) : '#';
    };

    details.onBoot = function (handler) {
        details.bootHandlers.push(handler);
    };

    details.boot = function () {
        details.bootHandlers.forEach(handler => handler());
    };
})(window);
