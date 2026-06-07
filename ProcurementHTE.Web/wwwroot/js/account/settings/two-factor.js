(function (window, document) {
    const config = window.accountSettingsConfig || {};
    const methodRadios = document.querySelectorAll('#twoFactorEnableForm input[name="method"]');
    const sendButton = document.getElementById('btnSendSetupCode');
    const codeHelp = document.getElementById('twoFactorCodeHelp');
    const authenticatorInfo = document.getElementById('authenticatorInfo');

    function syncTwoFactorUI() {
        const selected = document.querySelector('#twoFactorEnableForm input[name="method"]:checked')?.value;
        if (!selected || !sendButton || !codeHelp) return;

        if (selected === config.authenticatorMethod) {
            sendButton.classList.add('d-none');
            authenticatorInfo?.classList.remove('d-none');
            codeHelp.textContent = 'Masukkan kode dari aplikasi authenticator.';
            return;
        }

        sendButton.classList.remove('d-none');
        authenticatorInfo?.classList.add('d-none');
        sendButton.dataset.method = selected;
        codeHelp.textContent = `Kami akan mengirim kode via ${selected === config.emailMethod ? 'email' : 'SMS'}.`;
    }

    function sendSetupCode() {
        const method = sendButton?.dataset.method;
        if (!method || !config.sendCodeUrl) return;

        const token = document.querySelector('#twoFactorEnableForm input[name="__RequestVerificationToken"]')?.value;
        const body = new URLSearchParams();
        body.append('method', method);

        fetch(config.sendCodeUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                RequestVerificationToken: token ?? ''
            },
            body: body.toString()
        })
            .then(res => res.json())
            .then(showSendResult)
            .catch(() => Swal.fire({ icon: 'error', title: 'Gagal', text: 'Terjadi kesalahan saat mengirim kode.' }));
    }

    function showSendResult(response) {
        if (!response.success) {
            Swal.fire({ icon: 'error', title: 'Gagal', text: response.message ?? 'Tidak dapat mengirim kode.' });
            return;
        }

        if (response.devCode) {
            Swal.fire({
                icon: 'success',
                title: 'Kode dikirim',
                html: `<p class="mb-1">Gunakan kode berikut:</p><div class="fw-bold fs-4">${response.devCode}</div><small class="text-muted d-block mt-2">Kode ditampilkan untuk lingkungan pengujian.</small>`
            });
            return;
        }

        Swal.fire({ icon: 'success', title: 'Kode dikirim', text: response.message ?? 'Silakan cek email atau SMS Anda.' });
    }

    methodRadios.forEach(radio => radio.addEventListener('change', syncTwoFactorUI));
    syncTwoFactorUI();
    sendButton?.addEventListener('click', sendSetupCode);
})(window, document);
