using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class AccountService
{
    public Task<IReadOnlyList<UserSession>> GetSessionsAsync(
        string userId,
        CancellationToken ct = default
    ) => _sessionRepository.GetByUserAsync(userId, ct);

    public async Task<UserSession> RegisterSessionAsync(
        string userId,
        string? userAgent,
        string? ipAddress,
        string? device,
        string? browser,
        string? location,
        bool isCurrent,
        CancellationToken ct = default
    )
    {
        var session = new UserSession
        {
            UserId = userId,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            Device = device,
            Browser = browser,
            Location = location,
            IsCurrent = isCurrent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
        };

        await _sessionRepository.AddAsync(session, ct);
        await _sessionRepository.SaveAsync(ct);
        return session;
    }

    public async Task DeactivateSessionAsync(
        string userId,
        string sessionId,
        CancellationToken ct = default
    )
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is null || session.UserId != userId)
            return;

        session.IsActive = false;
        session.IsCurrent = false;

        await _sessionRepository.UpdateAsync(session, ct);
        await _sessionRepository.SaveAsync(ct);

        await LogEventAsync(
            userId,
            SecurityLogEventType.SessionRevoked,
            true,
            $"Sesi {session.Browser ?? session.Device ?? session.UserAgent} direvoke.",
            session.IpAddress,
            session.UserAgent,
            ct
        );
    }

    public async Task DeactivateAllSessionsAsync(string userId, CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetByUserAsync(userId, ct);
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.IsCurrent = false;
            await _sessionRepository.UpdateAsync(session, ct);
        }

        await _sessionRepository.SaveAsync(ct);
    }

    public Task<IReadOnlyList<UserSecurityLog>> GetSecurityLogsAsync(
        string userId,
        int take,
        CancellationToken ct = default
    ) => _logRepository.GetRecentAsync(userId, take, ct);

    public async Task LogEventAsync(
        string userId,
        SecurityLogEventType eventType,
        bool success,
        string? description,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default
    )
    {
        var log = new UserSecurityLog
        {
            UserId = userId,
            EventType = eventType,
            IsSuccess = success,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
        };

        await _logRepository.AddAsync(log, ct);
        await _logRepository.SaveAsync(ct);
    }
}
