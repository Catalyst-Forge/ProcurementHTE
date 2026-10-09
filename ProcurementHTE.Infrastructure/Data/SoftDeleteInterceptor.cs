using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

/// <summary>
/// Turns EF removals of <see cref="ISoftDeletable"/> entities into soft deletes
/// and stamps who/when on every deletion, so a plain <c>Remove()</c> anywhere in
/// the codebase can never physically delete these rows.
/// Bulk <c>ExecuteDelete</c> bypasses this on purpose.
/// </summary>
public sealed class SoftDeleteInterceptor(IServiceProvider services) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        context.ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        string? userId = null;
        var userResolved = false;
        string? CurrentUserId()
        {
            if (!userResolved)
            {
                userId = services.GetService<ICurrentUserAccessor>()?.UserId;
                userResolved = true;
            }
            return userId;
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            var entity = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entity.IsDeleted = true;
                    entity.DeletedAt ??= now;
                    entity.DeletedBy ??= CurrentUserId();
                    break;

                case EntityState.Modified when entity.IsDeleted:
                    entity.DeletedAt ??= now;
                    entity.DeletedBy ??= CurrentUserId();
                    break;

                case EntityState.Modified when entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified:
                    // Restored: clear the deletion stamp.
                    entity.DeletedAt = null;
                    entity.DeletedBy = null;
                    break;
            }
        }
    }
}
