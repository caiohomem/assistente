using MediatR;

namespace AssistenteExecutivo.Domain.DomainEvents;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}

