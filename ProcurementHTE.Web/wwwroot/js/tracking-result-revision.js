// TrackingResult Revision scripts
(function() {
    'use strict';

    document.addEventListener('DOMContentLoaded', function() {
        var submitRevisionBtn = document.getElementById('submitRevisionBtn');
        if (submitRevisionBtn) {
            submitRevisionBtn.addEventListener('click', function() {
                submitReturnForRevision();
            });
        }

        var resubmitBtn = document.getElementById('resubmitBtn');
        if (resubmitBtn) {
            resubmitBtn.addEventListener('click', function() {
                submitResubmitRevision(resubmitBtn.dataset.procurementId);
            });
        }
    });

    function submitReturnForRevision() {
        var form = document.getElementById('revisionForm');
        var procurementId = form.querySelector('input[name="procurementId"]').value;
        var rejectionNote = document.getElementById('rejectionNoteModal').value;
        
        // Collect checked symptoms
        var symptomsCheckboxes = form.querySelectorAll('input[name="symptoms"]:checked');
        var symptoms = [];
        symptomsCheckboxes.forEach(function(cb) {
            symptoms.push(parseInt(cb.value));
        });

        // Validation
        if (symptoms.length === 0) {
            Swal.fire({
                title: 'Pilih Masalah',
                text: 'Silakan pilih minimal satu masalah yang ditemukan.',
                icon: 'warning',
                confirmButtonColor: '#0d6efd'
            });
            return;
        }

        if (!rejectionNote || rejectionNote.trim() === '') {
            Swal.fire({
                title: 'Catatan Wajib Diisi',
                text: 'Silakan masukkan catatan rejection.',
                icon: 'warning',
                confirmButtonColor: '#0d6efd'
            });
            return;
        }

        // Confirm dialog
        Swal.fire({
            title: 'Konfirmasi Return for Revision',
            text: 'Procurement akan dikembalikan untuk perbaikan. Lanjutkan?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Ya, Return for Revision',
            cancelButtonText: 'Batal'
        }).then(function(result) {
            if (result.isConfirmed) {
                // Show loading
                Swal.fire({
                    title: 'Processing...',
                    text: 'Mengirim return for revision...',
                    allowOutsideClick: false,
                    didOpen: function() {
                        Swal.showLoading();
                    }
                });

                // Build form data
                var formData = new FormData();
                formData.append('procurementId', procurementId);
                formData.append('rejectionNote', rejectionNote);
                symptoms.forEach(function(s) {
                    formData.append('symptoms', s);
                });

                // Get anti-forgery token
                var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tokenInput) {
                    formData.append('__RequestVerificationToken', tokenInput.value);
                }

                // Submit via fetch
                fetch('/ProcurementTracking/ReturnForRevision', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                })
                .then(function(response) {
                    return response.json();
                })
                .then(function(data) {
                    if (data.success) {
                        Swal.fire({
                            title: 'Berhasil!',
                            text: data.message || 'Procurement berhasil dikembalikan untuk revisi.',
                            icon: 'success',
                            confirmButtonColor: '#198754'
                        }).then(function() {
                            // Close modal and reload page
                            var modal = bootstrap.Modal.getInstance(document.getElementById('rejectModal'));
                            if (modal) modal.hide();
                            window.location.reload();
                        });
                    } else {
                        Swal.fire({
                            title: 'Error',
                            text: data.message || 'Gagal mengirim return for revision.',
                            icon: 'error',
                            confirmButtonColor: '#dc3545'
                        });
                    }
                })
                .catch(function(error) {
                    Swal.fire({
                        title: 'Error',
                        text: 'Terjadi kesalahan: ' + error.message,
                        icon: 'error',
                        confirmButtonColor: '#dc3545'
                    });
                });
            }
        });
    }

    function submitResubmitRevision(procurementId) {
        Swal.fire({
            title: 'Konfirmasi Submit Revision',
            text: 'Apakah Anda yakin sudah melakukan perbaikan yang diperlukan?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Ya, Submit',
            cancelButtonText: 'Batal'
        }).then(function(result) {
            if (result.isConfirmed) {
                // Show loading
                Swal.fire({
                    title: 'Processing...',
                    text: 'Mengirim revision...',
                    allowOutsideClick: false,
                    didOpen: function() {
                        Swal.showLoading();
                    }
                });

                // Build form data
                var formData = new FormData();
                formData.append('procurementId', procurementId);

                // Get anti-forgery token from any form on page
                var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tokenInput) {
                    formData.append('__RequestVerificationToken', tokenInput.value);
                }

                // Submit via fetch
                fetch('/ProcurementTracking/ResubmitRevision', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                })
                .then(function(response) {
                    return response.json();
                })
                .then(function(data) {
                    if (data.success) {
                        Swal.fire({
                            title: 'Berhasil!',
                            text: data.message || 'Revision berhasil disubmit.',
                            icon: 'success',
                            confirmButtonColor: '#198754'
                        }).then(function() {
                            window.location.reload();
                        });
                    } else {
                        Swal.fire({
                            title: 'Error',
                            text: data.message || 'Gagal submit revision.',
                            icon: 'error',
                            confirmButtonColor: '#dc3545'
                        });
                    }
                })
                .catch(function(error) {
                    Swal.fire({
                        title: 'Error',
                        text: 'Terjadi kesalahan: ' + error.message,
                        icon: 'error',
                        confirmButtonColor: '#dc3545'
                    });
                });
            }
        });
    }
})();
