(function (window, document) {
    const details = window.ProcurementDetails;

    function initMobileActions() {
        const toggleButton = document.getElementById('buttonNavToggle');
        const buttonGroup = document.getElementById('groupNavButton');
        if (!toggleButton || !buttonGroup || toggleButton.dataset.boundToggle) return;

        toggleButton.dataset.boundToggle = '1';
        toggleButton.addEventListener('click', () => buttonGroup.classList.toggle('show'));
    }

    function initTabs() {
        const trigger = document.querySelector('#procurement-tab');
        if (trigger && !trigger.dataset.boundInitialTab) {
            trigger.dataset.boundInitialTab = '1';
            bootstrap.Tab.getOrCreateInstance(trigger).show();
        }
    }

    function confirmSubmit(options, formId) {
        Swal.fire(options).then(result => {
            if (result.isConfirmed) document.getElementById(formId)?.submit();
        });
    }

    function attachPublishHandler() {
        if (window.procurementPublishHandlerAttached) return;
        window.procurementPublishHandlerAttached = true;

        document.addEventListener('click', event => {
            const button = event.target.closest('.btn-publish');
            if (!button) return;

            event.preventDefault();
            confirmSubmit({
                title: 'Publish Procurement?',
                html: `Apakah Anda yakin ingin mempublish procurement <strong>${button.dataset.procnum}</strong>?<br><small class="text-muted">Status akan berubah menjadi "Waiting Pickup"</small>`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#198754',
                cancelButtonColor: '#6c757d',
                confirmButtonText: '<i class="bi bi-send-fill"></i> Ya, Publish',
                cancelButtonText: '<i class="bi bi-x"></i> Batal',
                reverseButtons: true
            }, 'publishForm');
        });
    }

    function attachUnpublishHandler() {
        if (window.procurementUnpublishHandlerAttached) return;
        window.procurementUnpublishHandlerAttached = true;

        document.addEventListener('click', event => {
            const button = event.target.closest('.btn-unpublish');
            if (!button) return;

            event.preventDefault();
            confirmSubmit({
                title: 'Batal Publish Procurement?',
                html: `Apakah Anda yakin ingin membatalkan publish procurement <strong>${button.dataset.procnum}</strong>?<br><small class="text-muted">Status akan kembali menjadi "Created" dan dapat diedit kembali</small>`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#ffc107',
                cancelButtonColor: '#6c757d',
                confirmButtonText: '<i class="bi bi-arrow-counterclockwise"></i> Ya, Batal Publish',
                cancelButtonText: '<i class="bi bi-x"></i> Tidak',
                reverseButtons: true
            }, 'unpublishForm');
        });
    }

    details.onBoot(() => {
        initMobileActions();
        initTabs();
        attachPublishHandler();
        attachUnpublishHandler();
    });
})(window, document);
