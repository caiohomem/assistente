using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.DomainEvents;

public record UserProvisionedFromKeycloak(
    Guid UserId,
    KeycloakSubject KeycloakSubject,
    DateTime OccurredAt) : IDomainEvent;

