using AssistenteExecutivo.Domain.Enums;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record DraftCreated(
    Guid DraftId,
    Guid OwnerUserId,
    Guid? ContactId,
    Guid? CompanyId,
    DocumentType DocumentType,
    DateTime OccurredAt) : IDomainEvent;









