(function (document) {
    function initializeOtpContainer(container) {
        const target = container.dataset.otpContainer;
        if (!target) return;

        const hidden = document.querySelector(`input[data-otp-hidden="${target}"]`);
        const inputs = Array.from(container.querySelectorAll('input[data-otp-input]'));
        if (!hidden || !inputs.length) return;

        const syncHidden = () => {
            hidden.value = inputs.map(input => input.value ?? '').join('');
        };

        const focusNext = index => {
            if (index + 1 < inputs.length) inputs[index + 1].focus();
        };

        const focusPrev = index => {
            if (index - 1 >= 0) inputs[index - 1].focus();
        };

        inputs.forEach((input, index) => bindOtpInput(input, index, inputs, syncHidden, focusNext, focusPrev));
        syncFromHidden(hidden, inputs);
    }

    function syncFromHidden(hidden, inputs) {
        const value = (hidden.value || '').replace(/[^0-9]/g, '');
        inputs.forEach((input, index) => {
            input.value = value[index] ?? '';
        });
    }

    function bindOtpInput(input, index, inputs, syncHidden, focusNext, focusPrev) {
        input.addEventListener('focus', () => input.select());
        input.addEventListener('keydown', event => handleNavigation(event, input, index, focusNext, focusPrev));
        input.addEventListener('input', event => {
            const value = event.target.value.replace(/[^0-9]/g, '');
            event.target.value = value.slice(-1);
            syncHidden();
            if (value.length) focusNext(index);
        });
        input.addEventListener('paste', event => {
            event.preventDefault();
            const pasted = (event.clipboardData?.getData('text') ?? '').replace(/[^0-9]/g, '');
            if (!pasted.length) return;

            inputs.forEach((current, currentIndex) => {
                current.value = pasted[currentIndex] ?? '';
            });
            syncHidden();
            inputs[Math.min(pasted.length, inputs.length - 1)]?.focus();
        });
    }

    function handleNavigation(event, input, index, focusNext, focusPrev) {
        if (event.key === 'Backspace' && !input.value) {
            event.preventDefault();
            focusPrev(index);
        }
        if (event.key === 'ArrowLeft') {
            event.preventDefault();
            focusPrev(index);
        }
        if (event.key === 'ArrowRight') {
            event.preventDefault();
            focusNext(index);
        }
    }

    document.querySelectorAll('[data-otp-container]').forEach(initializeOtpContainer);
})(document);
