using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class Milestone
{
    private Milestone() { } // EF Core

    private Milestone(
        Guid milestoneId,
        Guid agreementId,
        string description,
        Money value,
        DateTime dueDate,
        IClock clock)
    {
        if (milestoneId == Guid.Empty)
            throw new DomainException("Domain:MilestoneIdObrigatorio");

        if (agreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Domain:MilestoneDescricaoObrigatoria");

        if (value == null)
            throw new DomainException("Domain:MilestoneValorObrigatorio");

        if (dueDate == default)
            throw new DomainException("Domain:MilestoneDataObrigatoria");

        MilestoneId = milestoneId;
        AgreementId = agreementId;
        Description = description.Trim();
        Value = value;
        DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);
        Status = MilestoneStatus.Pending;
        CreatedAt = clock.UtcNow;
    }

    public Guid MilestoneId { get; private set; }
    public Guid AgreementId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Money Value { get; private set; } = null!;
    public DateTime DueDate { get; private set; }
    public MilestoneStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? CompletionNotes { get; private set; }
    public Guid? ReleasedPayoutTransactionId { get; private set; }

    public static Milestone Create(
        Guid milestoneId,
        Guid agreementId,
        string description,
        Money value,
        DateTime dueDate,
        IClock clock)
    {
        return new Milestone(milestoneId, agreementId, description, value, dueDate, clock);
    }

    internal void Complete(string? notes, Guid? releasedPayoutTransactionId, IClock clock)
    {
        if (Status == MilestoneStatus.Completed)
            return;

        Status = MilestoneStatus.Completed;
        CompletedAt = clock.UtcNow;
        CompletionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ReleasedPayoutTransactionId = releasedPayoutTransactionId;
    }

    internal void MarkOverdue()
    {
        if (Status == MilestoneStatus.Completed)
            return;

        Status = MilestoneStatus.Overdue;
    }

    internal void ResetStatus()
    {
        if (Status == MilestoneStatus.Pending)
            return;

        Status = MilestoneStatus.Pending;
        CompletedAt = null;
        CompletionNotes = null;
        ReleasedPayoutTransactionId = null;
    }
}
