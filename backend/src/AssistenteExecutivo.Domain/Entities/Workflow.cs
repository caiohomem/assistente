using AssistenteExecutivo.Domain.DomainEvents;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

/// <summary>
/// Aggregate root representing an automation workflow.
/// The workflow is defined by a WorkflowSpec JSON that is compiled to n8n format for execution.
/// </summary>
public class Workflow
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private Workflow() { } // EF Core

    public Workflow(
        Guid workflowId,
        Guid ownerUserId,
        string name,
        string specJson,
        WorkflowTrigger trigger,
        IClock clock)
    {
        if (workflowId == Guid.Empty)
            throw new DomainException("Domain:WorkflowIdObrigatorio");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Domain:OwnerUserIdObrigatorio");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:WorkflowNameObrigatorio");

        if (string.IsNullOrWhiteSpace(specJson))
            throw new DomainException("Domain:WorkflowSpecObrigatorio");

        if (trigger == null)
            throw new DomainException("Domain:WorkflowTriggerObrigatorio");

        WorkflowId = workflowId;
        OwnerUserId = ownerUserId;
        Name = name.Trim();
        SpecJson = specJson;
        SpecVersion = 1;
        Trigger = trigger;
        Status = WorkflowStatus.Draft;
        CreatedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public Guid WorkflowId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public WorkflowTrigger Trigger { get; private set; } = null!;
    public string SpecJson { get; private set; } = null!;
    public int SpecVersion { get; private set; }
    public string? N8nWorkflowId { get; private set; }
    public WorkflowStatus Status { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Factory method to create a new workflow.
    /// </summary>
    public static Workflow Create(
        Guid workflowId,
        Guid ownerUserId,
        string name,
        string specJson,
        WorkflowTrigger trigger,
        IClock clock)
    {
        var workflow = new Workflow(workflowId, ownerUserId, name, specJson, trigger, clock);
        workflow._domainEvents.Add(new WorkflowCreated(
            workflow.WorkflowId,
            workflow.OwnerUserId,
            workflow.Name,
            clock.UtcNow));
        return workflow;
    }

    /// <summary>
    /// Updates the workflow name.
    /// </summary>
    public void UpdateName(string name, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Domain:WorkflowNameObrigatorio");

        Name = name.Trim();
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Updates the workflow description.
    /// </summary>
    public void UpdateDescription(string? description, IClock clock)
    {
        Description = description?.Trim();
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Updates the workflow spec JSON. Increments version and clears n8n reference (needs recompilation).
    /// </summary>
    public void UpdateSpec(string specJson, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(specJson))
            throw new DomainException("Domain:WorkflowSpecObrigatorio");

        SpecJson = specJson;
        SpecVersion++;
        N8nWorkflowId = null; // Needs recompilation
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new WorkflowSpecUpdated(WorkflowId, SpecVersion, clock.UtcNow));
    }

    /// <summary>
    /// Updates the trigger configuration.
    /// </summary>
    public void UpdateTrigger(WorkflowTrigger trigger, IClock clock)
    {
        if (trigger == null)
            throw new DomainException("Domain:WorkflowTriggerObrigatorio");

        Trigger = trigger;
        N8nWorkflowId = null; // Needs recompilation
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Sets the n8n workflow ID after successful compilation and deployment.
    /// </summary>
    public void SetN8nWorkflowId(string n8nWorkflowId, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(n8nWorkflowId))
            throw new DomainException("Domain:N8nWorkflowIdObrigatorio");

        N8nWorkflowId = n8nWorkflowId;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Activates the workflow for execution. Requires n8n workflow to be compiled first.
    /// </summary>
    public void Activate(IClock clock)
    {
        if (string.IsNullOrWhiteSpace(N8nWorkflowId))
            throw new DomainException("Domain:WorkflowDeveSerCompiladoAntesDeAtivar");

        Status = WorkflowStatus.Active;
        UpdatedAt = clock.UtcNow;

        _domainEvents.Add(new WorkflowActivated(WorkflowId, clock.UtcNow));
    }

    /// <summary>
    /// Pauses the workflow temporarily.
    /// </summary>
    public void Pause(IClock clock)
    {
        if (Status != WorkflowStatus.Active)
            throw new DomainException("Domain:WorkflowSoPodeSerPausadoSeAtivo");

        Status = WorkflowStatus.Paused;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Resumes a paused workflow.
    /// </summary>
    public void Resume(IClock clock)
    {
        if (Status != WorkflowStatus.Paused)
            throw new DomainException("Domain:WorkflowSoPodeSerReativadoSePausado");

        Status = WorkflowStatus.Active;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Archives the workflow (soft delete).
    /// </summary>
    public void Archive(IClock clock)
    {
        Status = WorkflowStatus.Archived;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Clears all domain events after publishing.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Sets the idempotency key for duplicate detection.
    /// </summary>
    public void SetIdempotencyKey(string? idempotencyKey)
    {
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>
    /// Binds this workflow to an n8n workflow after compilation.
    /// </summary>
    public void BindToN8n(string n8nWorkflowId, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(n8nWorkflowId))
            throw new DomainException("Domain:N8nWorkflowIdObrigatorio");

        N8nWorkflowId = n8nWorkflowId;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Alternative constructor for creating workflows from specs.
    /// </summary>
    public Workflow(
        string name,
        Guid ownerUserId,
        string specJson,
        WorkflowTrigger trigger,
        IClock clock)
        : this(Guid.NewGuid(), ownerUserId, name, specJson, trigger, clock)
    {
    }
}
