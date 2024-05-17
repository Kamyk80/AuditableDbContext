namespace AuditableDbContext;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
public class NotAuditableAttribute : Attribute
{
}