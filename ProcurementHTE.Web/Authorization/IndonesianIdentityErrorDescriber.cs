using Microsoft.AspNetCore.Identity;

namespace ProcurementHTE.Web.Authorization;

/// <summary>
/// Puts the Identity errors that reach a form into Indonesian, since the framework
/// only ships them in English. UserName always holds the email, so its errors speak of the email.
/// </summary>
public sealed class IndonesianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Email '{userName}' sudah dipakai.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"Email '{email}' sudah dipakai.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), $"Email '{userName}' memakai karakter yang tidak didukung.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), $"Email '{email}' tidak valid.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Password salah.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Password minimal {length} karakter.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"Password harus memakai minimal {uniqueChars} karakter berbeda.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Password harus memakai minimal satu simbol.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Password harus memakai minimal satu angka.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Password harus memakai minimal satu huruf kecil.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Password harus memakai minimal satu huruf besar.");

    private static IdentityError Error(string code, string description) =>
        new() { Code = code, Description = description };
}
