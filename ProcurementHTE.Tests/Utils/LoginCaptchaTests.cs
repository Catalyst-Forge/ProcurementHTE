using Microsoft.AspNetCore.Http;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Tests.Utils;

public class LoginCaptchaTests
{
    [Fact]
    public void Create_Addition_ReturnsSum()
    {
        var values = new Queue<int>([8, 3, 0]);

        var challenge = LoginCaptcha.Create((_, _) => values.Dequeue());

        Assert.Equal("8 + 3", challenge.Question);
        Assert.Equal(11, challenge.Answer);
    }

    [Fact]
    public void Create_Subtraction_NeverGoesNegative()
    {
        var values = new Queue<int>([3, 8, 1]);

        var challenge = LoginCaptcha.Create((_, _) => values.Dequeue());

        Assert.Equal("8 − 3", challenge.Question);
        Assert.Equal(5, challenge.Answer);
    }

    [Fact]
    public void Verify_AcceptsCorrectAnswerOnlyOnce()
    {
        var session = new FakeSession();
        LoginCaptcha.Issue(session);
        var expected = session.GetInt32("Auth.LoginCaptcha");

        Assert.True(LoginCaptcha.Verify(session, expected));
        Assert.False(LoginCaptcha.Verify(session, expected));
    }

    [Fact]
    public void Verify_ConsumesChallengeEvenWhenAnswerIsWrong()
    {
        var session = new FakeSession();
        LoginCaptcha.Issue(session);
        var expected = session.GetInt32("Auth.LoginCaptcha");

        Assert.False(LoginCaptcha.Verify(session, expected + 1));
        Assert.False(LoginCaptcha.Verify(session, expected));
    }

    [Fact]
    public void Verify_RejectsWhenNoChallengeWasIssued()
    {
        Assert.False(LoginCaptcha.Verify(new FakeSession(), 4));
    }

    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "test";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
