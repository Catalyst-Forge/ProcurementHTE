namespace ProcurementHTE.Core.Interfaces;

/// <summary>The signed-in user for the current request, if any.</summary>
public interface ICurrentUserAccessor
{
    string? UserId { get; }
}
