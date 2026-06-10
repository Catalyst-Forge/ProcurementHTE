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

})();
