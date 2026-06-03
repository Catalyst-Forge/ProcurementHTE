// TrackingResult page scripts
(function() {
    'use strict';

    window.printQrCode = function(qrUrl, procNum) {
        var w = window.open('', '_blank', 'width=400,height=500');
        if (!w) {
            alert('Popup blocked. Mohon izinkan popup untuk mencetak QR.');
            return;
        }
        var htmlContent = '<!DOCTYPE html><html><head><title>QR Code - ' + procNum + '</title>' +
            '<style>body{display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;margin:0;font-family:Arial,sans-serif;}h3{margin-bottom:10px;}p{margin:5px 0;color:#666;}img{max-width:300px;margin:20px 0;}</style>' +
            '</head><body><h3>QR Code Approval</h3><p>' + procNum + '</p>' +
            '<img src="' + qrUrl + '" alt="QR Code" onload="window.print()"></body></html>';
        w.document.write(htmlContent);
        w.document.close();
    };

    window.confirmApprove = function() {
        Swal.fire({
            title: 'Konfirmasi Approval',
            text: 'Apakah Anda yakin ingin meng-approve procurement ini?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Ya, Approve',
            cancelButtonText: 'Batal'
        }).then(function(result) {
            if (result.isConfirmed) {
                document.getElementById('approveForm').submit();
            }
        });
    };

    window.confirmReject = function() {
        var rejectionNote = document.getElementById('rejectionNote').value;
        if (!rejectionNote || rejectionNote.trim() === '') {
            Swal.fire({
                title: 'Alasan Wajib Diisi',
                text: 'Silakan masukkan alasan reject terlebih dahulu.',
                icon: 'warning',
                confirmButtonColor: '#0d6efd'
            });
            return;
        }

        Swal.fire({
            title: 'Konfirmasi Reject',
            text: 'Apakah Anda yakin ingin me-reject procurement ini?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Ya, Reject',
            cancelButtonText: 'Batal'
        }).then(function(result) {
            if (result.isConfirmed) {
                document.getElementById('rejectForm').submit();
            }
        });
    };

    window.showHardcopyEvidence = function(procurementId) {
        // Show loading
        Swal.fire({
            title: 'Loading...',
            text: 'Mengambil gambar hardcopy evidence...',
            allowOutsideClick: false,
            didOpen: function() {
                Swal.showLoading();
            }
        });

        // Fetch presigned URL from API
        fetch('/ProcurementTracking/HardcopyEvidence/' + procurementId)
            .then(function(response) {
                if (!response.ok) throw new Error('Hardcopy evidence tidak ditemukan');
                return response.json();
            })
            .then(function(data) {
                Swal.fire({
                    title: 'Hardcopy Evidence',
                    html: '<img src="' + data.url + '" class="img-fluid rounded" style="max-height: 70vh;" alt="Hardcopy Evidence">',
                    width: '80%',
                    showCloseButton: true,
                    showConfirmButton: false,
                    customClass: {
                        popup: 'swal-wide'
                    }
                });
            })
            .catch(function(error) {
                Swal.fire({
                    title: 'Error',
                    text: error.message || 'Gagal mengambil hardcopy evidence',
                    icon: 'error'
                });
            });
    };

    // Return for Revision form handler
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
