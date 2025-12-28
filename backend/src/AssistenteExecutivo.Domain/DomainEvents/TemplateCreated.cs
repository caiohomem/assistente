using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record TemplateCreated(
    Guid TemplateId,
    Guid OwnerUserId,
    string Name,
    TemplateType Type,
    DateTime OccurredAt) : IDomainEvent;





