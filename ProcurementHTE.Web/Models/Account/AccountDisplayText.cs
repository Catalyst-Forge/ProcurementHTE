using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Web.Models.Account
{
    /// <summary>Human-readable Indonesian labels for account enums shown on the settings page.</summary>
    public static class AccountDisplayText
    {
        public static string ForTwoFactorMethod(TwoFactorMethod method) =>
            method switch
            {
                TwoFactorMethod.AuthenticatorApp => "Aplikasi authenticator",
                TwoFactorMethod.Email => "Kode via email",
                TwoFactorMethod.Sms => "Kode via SMS",
                _ => "Belum dipilih",
            };

        public static string ForSecurityEvent(SecurityLogEventType type) =>
            type switch
            {
                SecurityLogEventType.LoginSuccess => "Login berhasil",
                SecurityLogEventType.LoginFailed => "Login gagal",
                SecurityLogEventType.PasswordChanged => "Password diganti",
                SecurityLogEventType.PasswordChangeFailed => "Gagal mengganti password",
                SecurityLogEventType.TwoFactorEnabled => "2FA diaktifkan",
                SecurityLogEventType.TwoFactorDisabled => "2FA dinonaktifkan",
                SecurityLogEventType.TwoFactorMethodChanged => "Metode 2FA diganti",
                SecurityLogEventType.ProfileUpdated => "Profil diperbarui",
                SecurityLogEventType.AvatarUpdated => "Foto profil diganti",
                SecurityLogEventType.LogoutAllSessions => "Logout dari semua perangkat",
                SecurityLogEventType.SessionRevoked => "Sesi perangkat diakhiri",
                SecurityLogEventType.Logout => "Logout",
                SecurityLogEventType.EmailVerified => "Email terverifikasi",
                SecurityLogEventType.PhoneVerified => "Nomor HP terverifikasi",
                _ => type.ToString(),
            };

        public static string IconForSecurityEvent(SecurityLogEventType type) =>
            type switch
            {
                SecurityLogEventType.LoginSuccess or SecurityLogEventType.LoginFailed => "bi-box-arrow-in-right",
                SecurityLogEventType.PasswordChanged or SecurityLogEventType.PasswordChangeFailed => "bi-key",
                SecurityLogEventType.TwoFactorEnabled
                    or SecurityLogEventType.TwoFactorDisabled
                    or SecurityLogEventType.TwoFactorMethodChanged => "bi-shield-lock",
                SecurityLogEventType.ProfileUpdated or SecurityLogEventType.AvatarUpdated => "bi-person",
                SecurityLogEventType.LogoutAllSessions
                    or SecurityLogEventType.SessionRevoked
                    or SecurityLogEventType.Logout => "bi-box-arrow-right",
                SecurityLogEventType.EmailVerified => "bi-envelope-check",
                SecurityLogEventType.PhoneVerified => "bi-phone",
                _ => "bi-dot",
            };
    }
}
