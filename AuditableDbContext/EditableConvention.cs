using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AuditableDbContext;
internal class EditableConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (typeof(IEditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                SetMaxLength(entityType, nameof(IEditableEntity.CreatedBy));
                SetMaxLength(entityType, nameof(IEditableEntity.UpdatedBy));
            }
        }
    }

    private static void SetMaxLength(IConventionEntityType entityType, string propertyName) =>
        entityType.FindProperty(propertyName)?.Builder.HasMaxLength(254);
}