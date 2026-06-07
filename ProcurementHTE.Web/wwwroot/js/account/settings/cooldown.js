(function (document) {
    function startCooldown(button, seconds, statusSelector) {
        if (!button || seconds <= 0 || button.dataset.cooldownActive === '1') return;

        button.dataset.cooldownActive = '1';
        button.disabled = true;

        let remaining = seconds;
        const statusElement = statusSelector ? document.querySelector(statusSelector) : null;

        function updateStatus() {
            if (!statusElement) return;
            statusElement.textContent = `Tunggu ${remaining} detik sebelum mengirim ulang.`;
            statusElement.classList.add('text-warning');
        }

        updateStatus();

        const intervalId = setInterval(() => {
            remaining--;
            if (remaining <= 0) {
                clearInterval(intervalId);
                button.disabled = false;
                button.dataset.cooldownActive = '';
                if (statusElement) {
                    statusElement.textContent = '';
                    statusElement.classList.remove('text-warning');
                }
                return;
            }

            updateStatus();
        }, 1000);
    }

    document.querySelectorAll('[data-cooldown-button]').forEach(button => {
        const seconds = parseInt(button.dataset.cooldownSeconds ?? '0', 10);
        if (seconds > 0) startCooldown(button, seconds, button.dataset.statusTarget);
    });
})(document);
