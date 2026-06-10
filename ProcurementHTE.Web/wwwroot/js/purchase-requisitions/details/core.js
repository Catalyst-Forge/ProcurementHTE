(function (window) {
    const details = window.PrDetails = window.PrDetails || {};
    details.state = details.state || {
        currentPreviewUrl: null,
        currentDownloadDocId: null
    };
    details.bootHandlers = details.bootHandlers || [];

    details.config = function () {
        return window.prDetailsConfig || {};
    };

    details.prId = function () {
        return details.config().prId || '';
    };

    details.maxUploadSize = function () {
        return details.config().maxUploadSize || 10 * 1024 * 1024;
    };

    details.token = function () {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    };

    details.showToast = function (icon, title, text = '', timer = 4000) {
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
            didOpen: element => {
                element.addEventListener('mouseenter', Swal.stopTimer);
                element.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });
    };

    details.bindOnce = function (selector, flag, binder) {
        document.querySelectorAll(selector).forEach(element => {
            if (element.dataset[flag]) return;
            element.dataset[flag] = '1';
            binder(element);
        });
    };

    details.onBoot = function (handler) {
        details.bootHandlers.push(handler);
    };

    details.boot = function () {
        details.bootHandlers.forEach(handler => handler());
    };

    details.formatSize = function (sizeBytes) {
        const sizeKb = Number(sizeBytes || 0) / 1024;
        const sizeMb = sizeKb / 1024;
        return sizeMb >= 1 ? `${sizeMb.toFixed(2)} MB` : `${sizeKb.toFixed(2)} KB`;
    };

    details.escapeAttr = function (value) {
        return String(value || '').replace(/"/g, '&quot;');
    };

    details.escapeHtml = function (value) {
        const element = document.createElement('span');
        element.textContent = String(value || '');
        return element.innerHTML;
    };

    window.showToast = details.showToast;
})(window);
