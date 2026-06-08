// Handle logout to notify SignalR before disconnecting
(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    const logoutForm = document.getElementById("logout-form");

    if (logoutForm) {
      logoutForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        console.log("Logout form submitted - notifying SignalR...");

        // Try to notify SignalR of logout
        if (window.dashboardConnection && window.dashboardConnection.state === 0) {
          // 0 = Connected state
          try {
            await window.dashboardConnection.invoke("NotifyLogout");
            console.log("✓ SignalR notified of logout");
          } catch (err) {
            console.error("Failed to notify SignalR:", err);
          }

          // Give it a moment to broadcast
          await new Promise((resolve) => setTimeout(resolve, 200));
        }

        // Now submit the form
        console.log("Proceeding with logout...");
        logoutForm.submit();
      });
    }
  });
})();

// Global function for Send for Approval with SweetAlert2
function confirmSendApproval(prId, prNumber) {
  Swal.fire({
    title: '<i class="bi bi-send-fill text-primary"></i> Send for Approval?',
    html: `
      <div class="text-start">
        <p class="mb-2">PR <strong>${prNumber}</strong> akan dikirim untuk approval.</p>
        <div class="alert alert-info py-2 mb-0">
          <i class="bi bi-qr-code me-2"></i>
          <small>QR Code akan di-generate untuk proses approval</small>
        </div>
      </div>
    `,
    icon: 'question',
    showCancelButton: true,
    confirmButtonColor: '#0d6efd',
    cancelButtonColor: '#6c757d',
    confirmButtonText: '<i class="bi bi-send-fill me-1"></i> Ya, Kirim',
    cancelButtonText: '<i class="bi bi-x-lg me-1"></i> Batal',
    reverseButtons: true,
    focusCancel: true
  }).then((result) => {
    if (result.isConfirmed) {
      // Show loading
      Swal.fire({
        title: 'Mengirim...',
        html: 'Sedang memproses dan generate QR Code',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.showLoading();
        }
      });
      const form = document.getElementById('sendApprovalForm-' + prId);
      if (form) {
        form.submit();
      }
    }
  });
}
