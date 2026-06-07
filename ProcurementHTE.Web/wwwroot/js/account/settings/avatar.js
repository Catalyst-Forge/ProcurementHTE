(function (window, document) {
    const avatarInput = document.getElementById('avatarFile');
    const imageDataInput = document.getElementById('avatarImageData');
    const modalElement = document.getElementById('avatarCropModal');
    const cropImage = document.getElementById('avatarCropImage');
    const avatarForm = document.getElementById('avatarForm');
    const livePreview = document.querySelector('.avatar-live-preview');

    if (!avatarInput || !imageDataInput || !modalElement || !cropImage || !avatarForm) return;

    const modal = new bootstrap.Modal(modalElement);
    let cropper;

    avatarInput.addEventListener('change', event => {
        const file = event.target.files?.[0];
        if (!file) return;

        if (!file.type.startsWith('image/')) {
            Swal.fire({ icon: 'error', title: 'Format tidak didukung', text: 'Pilih file gambar (JPG/PNG).' });
            event.target.value = '';
            return;
        }

        const reader = new FileReader();
        reader.onload = e => {
            cropImage.src = e.target?.result;
            modal.show();
        };
        reader.readAsDataURL(file);
    });

    modalElement.addEventListener('shown.bs.modal', () => {
        cropper = new Cropper(cropImage, {
            aspectRatio: 1,
            viewMode: 2,
            dragMode: 'move',
            autoCropArea: 1,
            background: false,
            responsive: true,
            checkOrientation: true,
            preview: livePreview ? '.avatar-live-preview' : undefined
        });
    });

    modalElement.addEventListener('hidden.bs.modal', () => {
        cropper?.destroy();
        cropper = null;
        avatarInput.value = '';
    });

    document.getElementById('saveCroppedAvatar')?.addEventListener('click', () => {
        if (!cropper) return;

        const canvas = cropper.getCroppedCanvas({
            width: 480,
            height: 480,
            imageSmoothingQuality: 'high'
        });
        canvas.toBlob(blob => {
            if (!blob) return;

            const reader = new FileReader();
            reader.onloadend = () => {
                imageDataInput.value = reader.result;
                modal.hide();
                avatarForm.submit();
            };
            reader.readAsDataURL(blob);
        }, 'image/png');
    });
})(window, document);
