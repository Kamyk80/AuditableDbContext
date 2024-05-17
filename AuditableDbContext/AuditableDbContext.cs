using Microsoft.EntityFrameworkCore;

namespace AuditableDbContext;

public class AuditableDbContext : DbContext
{
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    public AuditableDbContext(DbContextOptions options)
        : base(options)
    {
    }

    [Obsolete("Use the audited version of SaveChanges instead.", true)]
    public override int SaveChanges() =>
        throw new InvalidOperationException("Use the audited version of SaveChanges instead.");

    public int SaveChanges(string username)
    {
        var dynamicAuditEntries = HandleAuditBeforeSave(username, DateTimeOffset.Now);
        if (dynamicAuditEntries.Count > 0)
        {
            return TwoPhaseTransactionSaveChanges(dynamicAuditEntries);
        }

        return base.SaveChanges();
    }

    private int TwoPhaseTransactionSaveChanges(IEnumerable<AuditEntry> auditEntries)
    {
        if (Database.CurrentTransaction is null)
        {
            using var transaction = Database.BeginTransaction();
            var result = TwoPhaseSaveChanges(auditEntries);
            transaction.Commit();
            return result;
        }

        return TwoPhaseSaveChanges(auditEntries);
    }

    private int TwoPhaseSaveChanges(IEnumerable<AuditEntry> auditEntries)
    {
        var result = base.SaveChanges();
        HandleAuditAfterSave(auditEntries);
        base.SaveChanges();
        return result;
    }

    [Obsolete("Use the audited version of SaveChangesAsync instead.", true)]
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Use the audited version of SaveChangesAsync instead.");

    public async Task<int> SaveChangesAsync(string username, CancellationToken cancellationToken = default)
    {
        var dynamicAuditEntries = HandleAuditBeforeSave(username, DateTimeOffset.Now);
        if (dynamicAuditEntries.Count > 0)
        {
            return await TwoPhaseTransactionSaveChangesAsync(dynamicAuditEntries, cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> TwoPhaseTransactionSaveChangesAsync(IEnumerable<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is null)
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            var result = await TwoPhaseSaveChangesAsync(auditEntries, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        return await TwoPhaseSaveChangesAsync(auditEntries, cancellationToken);
    }

    private async Task<int> TwoPhaseSaveChangesAsync(IEnumerable<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        HandleAuditAfterSave(auditEntries);
        await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    private ICollection<AuditEntry> HandleAuditBeforeSave(string username, DateTimeOffset timestamp)
    {
        var changedEntries = ChangeTracker.Entries<IEditableEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in changedEntries)
        {
            entry.Property("UpdatedBy").CurrentValue = username;
            entry.Property("UpdatedOn").CurrentValue = timestamp;

            if (entry.State is EntityState.Added)
            {
                entry.Property("CreatedBy").CurrentValue = username;
                entry.Property("CreatedOn").CurrentValue = timestamp;
            }
        }

        var auditEntries = ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => !entry.Metadata.ClrType.IsDefined(typeof(NotAuditableAttribute), false))
            .Select(entry => new AuditEntry(entry, username, timestamp))
            .ToList();

        var dynamicAuditEntries = new List<AuditEntry>();

        foreach (var auditEntry in auditEntries)
        {
            if (auditEntry.HasGeneratedProperties())
            {
                dynamicAuditEntries.Add(auditEntry);
            }
            else
            {
                AuditLogs.Add(auditEntry.ToAuditLog());
            }
        }

        return dynamicAuditEntries;
    }

    private void HandleAuditAfterSave(IEnumerable<AuditEntry> auditEntries)
    {
        foreach (var auditEntry in auditEntries)
        {
            auditEntry.UpdateGeneratedProperties();
            AuditLogs.Add(auditEntry.ToAuditLog());
        }
    }
}