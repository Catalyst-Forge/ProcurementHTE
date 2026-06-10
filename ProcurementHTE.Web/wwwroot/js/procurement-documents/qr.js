(function (window) {
    const docs = window.ProcurementDocuments;

    async function showQrCode(docId) {
        try {
            const response = await fetch(`/ProcurementDocuments/QrUrl/${encodeURIComponent(docId)}`);
            const data = await response.json();
            if (!data.ok || !data.url) throw new Error(data.error || 'Failed to build the QR URL');

            docs.state.currentQrImageSrc = data.url;
            document.getElementById('qrImg').src = data.url;
            document.getElementById('btnDownloadQr').href =
                `/ProcurementDocuments/DownloadQr/${encodeURIComponent(docId)}`;

            new bootstrap.Modal(document.getElementById('qrModal')).show();
        } catch (error) {
            console.error('QR error:', error);
            docs.showToast('error', 'Failed to load QR', error.message, 3000);
        }
    }

    function printQrCode() {
        if (!docs.state.currentQrImageSrc) return;

        const target = window.open('', '_blank', 'width=600,height=600');
        const doc = target.document;
        doc.title = 'Print QR Code';
        if (!doc.head) doc.documentElement.appendChild(doc.createElement('head'));

        const style = doc.createElement('style');
        style.textContent = 'html,body{height:100%;margin:0}body{display:flex;align-items:center;justify-content:center}img{max-width:90%;max-height:90%}';
        doc.head.appendChild(style);
        doc.body.innerHTML = '';

        const img = doc.createElement('img');
        img.id = 'qrToPrint';
        img.alt = 'QR Code';
        img.src = docs.state.currentQrImageSrc;
        doc.body.appendChild(img);
        img.onload = function () {
            target.focus();
            target.print();
        };
    }

    function initQrHandlers() {
        docs.bindOnce('.btn-show-qr', 'boundShowQr', btn => {
            btn.addEventListener('click', async function () {
                await showQrCode(this.dataset.docId);
            });
        });

        const printBtn = document.getElementById('btnPrintQr');
        if (printBtn && !printBtn.dataset.boundPrintQr) {
            printBtn.dataset.boundPrintQr = '1';
            printBtn.addEventListener('click', printQrCode);
        }
    }

    window.showQrCode = showQrCode;
    window.printQrCode = printQrCode;
    docs.onBoot(initQrHandlers);
})(window);
