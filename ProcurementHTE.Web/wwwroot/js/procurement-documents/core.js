(function (window) {
    const docs = window.ProcurementDocuments = window.ProcurementDocuments || {};
    docs.state = docs.state || {
        currentPreviewUrl: null,
        currentDownloadDocId: null,
        currentQrImageSrc: null
    };
    docs.bootHandlers = docs.bootHandlers || [];

    docs.config = function () {
        return window.procurementDocumentsConfig || {};
    };

    docs.procurementId = function () {
        return docs.config().procurementId || '';
    };

    docs.maxUploadSize = function () {
        return docs.config().maxUploadSize || 10 * 1024 * 1024;
    };

    docs.showToast = function (icon, title, text = '', timer = 4000) {
        if (!window.Swal) {
            if (title || text) window.alert([title, text].filter(Boolean).join('\n'));
            return;
        }

        Swal.fire({
            toast: true,
            position: 'top-end',
            icon,
            title,
            text,
            showConfirmButton: false,
            timer,
            timerProgressBar: true,
            didOpen: t => {
                t.addEventListener('mouseenter', Swal.stopTimer);
                t.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });
    };

    docs.bindOnce = function (selector, flag, binder) {
        document.querySelectorAll(selector).forEach(el => {
            if (el.dataset[flag]) return;
            el.dataset[flag] = '1';
            binder(el);
        });
    };

    docs.onBoot = function (handler) {
        docs.bootHandlers.push(handler);
    };

    docs.boot = function () {
        docs.bootHandlers.forEach(handler => handler());
    };

    docs.escapeHtml = function (value) {
        if (!value) return '';
        return String(value).replace(/[&<>"']/g, m => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[m]));
    };

    window.showToast = docs.showToast;
})(window);
