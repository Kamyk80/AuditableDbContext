using System.Collections.Specialized;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuditableDbContext;

internal class AuditEntry
{
    private readonly string _username;
    private readonly DateTimeOffset _timestamp;
    private readonly string _entity;
    private readonly string _operation;
    private int? _entityId;
    private readonly OrderedDictionary _oldValues = new();
    private readonly OrderedDictionary _newValues = new();
    private readonly List<PropertyEntry> _generatedProperties = new();
    private readonly List<string> _changes = new();

    public AuditEntry(EntityEntry entry, string username, DateTimeOffset timestamp)
    {
        _username = username;
        _timestamp = timestamp;

        _entity = entry.Entity.GetType().Name;
        _operation = entry.State.ToString();

        var hasCompositeKey = entry.Metadata.FindPrimaryKey()?.Properties.Count > 1;

        foreach (var property in entry.Properties)
        {
            if (!hasCompositeKey)
            {
                TryExtractEntityId(property);
            }

            TryExtractOldValues(entry, property);
            TryExtractNewValues(entry, property);
            TryExtractChanges(entry, property);
        }
    }

    private void TryExtractEntityId(PropertyEntry property)
    {
        if (property.Metadata.IsPrimaryKey())
        {
            _entityId = property.CurrentValue as int?;
        }
    }

    private void TryExtractOldValues(EntityEntry entry, PropertyEntry property)
    {
        if (entry.State is EntityState.Deleted or EntityState.Modified && IsAuditable(property))
        {
            _oldValues[property.Metadata.Name] = property.OriginalValue;
        }
    }

    private void TryExtractNewValues(EntityEntry entry, PropertyEntry property)
    {
        if (entry.State is EntityState.Modified or EntityState.Added && IsAuditable(property))
        {
            _newValues[property.Metadata.Name] = property.CurrentValue;

            if (property.IsTemporary || IsValueGeneratedOnAdd(entry, property) || IsValueGeneratedOnUpdate(entry, property))
            {
                _generatedProperties.Add(property);
            }
        }
    }

    private static bool IsAuditable(PropertyEntry property) =>
        !property.Metadata.PropertyInfo?.IsDefined(typeof(NotAuditableAttribute), false) ??
        !property.Metadata.FieldInfo?.IsDefined(typeof(NotAuditableAttribute), false) ??
        true;

    private static bool IsValueGeneratedOnAdd(EntityEntry entry, PropertyEntry property) =>
        entry.State is EntityState.Added &&
        property.Metadata.ValueGenerated.HasFlag(ValueGenerated.OnAdd);

    private static bool IsValueGeneratedOnUpdate(EntityEntry entry, PropertyEntry property) =>
        entry.State is EntityState.Modified &&
        (property.Metadata.ValueGenerated.HasFlag(ValueGenerated.OnUpdate) ||
         property.Metadata.ValueGenerated.HasFlag(ValueGenerated.OnUpdateSometimes));

    private void TryExtractChanges(EntityEntry entry, PropertyEntry property)
    {
        if (entry.State is EntityState.Modified && property.IsModified)
        {
            _changes.Add(property.Metadata.Name);
        }
    }

    public bool HasGeneratedProperties() => _generatedProperties.Count > 0;

    public AuditLog ToAuditLog()
    {
        return new AuditLog
        {
            Username = _username,
            Timestamp = _timestamp,
            Entity = _entity,
            Operation = _operation,
            EntityId = _entityId,
            OldValues = _oldValues.Count > 0 ? JsonSerializer.Serialize(_oldValues) : null,
            NewValues = _newValues.Count > 0 ? JsonSerializer.Serialize(_newValues) : null,
            Changes = _changes.Count > 0 ? JsonSerializer.Serialize(_changes) : null
        };
    }

    public void UpdateGeneratedProperties()
    {
        foreach (var property in _generatedProperties)
        {
            TryExtractEntityId(property);

            _newValues[property.Metadata.Name] = property.CurrentValue;
        }
    }
}