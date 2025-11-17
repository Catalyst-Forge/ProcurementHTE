using System;
using Microsoft.AspNetCore.Http;

namespace ProcurementHTE.Web.Helpers
{
    public static class CooldownHelper
    {
        private const string Prefix = "Cooldown.";

        public static bool IsInCooldown(ISession session, string purpose, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (session == null)
                return false;

            var key = Prefix + purpose;
            var raw = session.GetString(key);
            if (string.IsNullOrWhiteSpace(raw) || !long.TryParse(raw, out var ticks))
            {
                session?.Remove(key);
                return false;
            }

            var expireAt = new DateTime(ticks, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            if (now >= expireAt)
            {
                session.Remove(key);
                return false;
            }

            remaining = expireAt - now;
            return true;
        }

        public static void SetCooldown(ISession session, string purpose, TimeSpan duration)
        {
            if (session == null)
                return;

            var key = Prefix + purpose;
            var expireAt = DateTime.UtcNow.Add(duration);
            session.SetString(key, expireAt.Ticks.ToString());
        }

        public static int GetRemainingSeconds(ISession session, string purpose)
        {
            if (IsInCooldown(session, purpose, out var remaining))
                return (int)Math.Ceiling(remaining.TotalSeconds);

            return 0;
        }
    }
}
