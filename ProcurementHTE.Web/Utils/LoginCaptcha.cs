using System.Security.Cryptography;

namespace ProcurementHTE.Web.Utils;

/// <summary>
/// Simple arithmetic captcha for the login form. The expected answer lives only
/// in the server session and is consumed by every verification attempt, so a
/// solved question can never be replayed.
/// </summary>
public static class LoginCaptcha
{
    private const string SessionKey = "Auth.LoginCaptcha";

    public sealed record Challenge(string Question, int Answer);

    public static Challenge Create(Func<int, int, int>? nextInt = null)
    {
        nextInt ??= RandomNumberGenerator.GetInt32;

        var left = nextInt(1, 10);
        var right = nextInt(1, 10);

        // Subtraction always keeps the larger operand first so answers stay non-negative.
        if (nextInt(0, 2) == 0)
            return new Challenge($"{left} + {right}", left + right);

        var (high, low) = left >= right ? (left, right) : (right, left);
        return new Challenge($"{high} − {low}", high - low);
    }

    public static string Issue(ISession session)
    {
        var challenge = Create();
        session.SetInt32(SessionKey, challenge.Answer);
        return challenge.Question;
    }

    public static bool Verify(ISession session, int? answer)
    {
        var expected = session.GetInt32(SessionKey);
        session.Remove(SessionKey);
        return expected.HasValue && answer.HasValue && expected.Value == answer.Value;
    }
}
