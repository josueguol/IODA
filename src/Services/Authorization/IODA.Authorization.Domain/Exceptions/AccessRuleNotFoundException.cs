using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Authorization.Domain.Exceptions;

public class AccessRuleNotFoundException : DomainException
{
    public Guid AccessRuleId { get; }

    public AccessRuleNotFoundException(Guid accessRuleId)
        : base($"Access rule with id '{accessRuleId}' was not found.")
    {
        AccessRuleId = accessRuleId;
    }
}
