namespace ProcurementHTE.Core.Models.Enums
{
    public enum SecurityLogEventType
    {
        LoginSuccess = 0,
        LoginFailed = 1,
        PasswordChanged = 2,
        PasswordChangeFailed = 3,
        TwoFactorEnabled = 4,
        TwoFactorDisabled = 5,
        TwoFactorMethodChanged = 6,
        ProfileUpdated = 7,
        AvatarUpdated = 8,
        LogoutAllSessions = 9,
        SessionRevoked = 10,
        Logout = 11,
        EmailVerified = 12,
        PhoneVerified = 13,
    }
}
