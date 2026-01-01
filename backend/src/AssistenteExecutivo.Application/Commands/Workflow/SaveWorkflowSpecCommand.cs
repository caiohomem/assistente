using MediatR;

namespace AssistenteExecutivo.Application.Commands.Workflow;

public class SaveWorkflowSpecCommand : IRequest<SaveWorkflowSpecResult>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SpecJson { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}

public class SaveWorkflowSpecResult
{
    public bool Success { get; set; }
    public Guid? SpecId { get; set; }
    public int SpecVersion { get; set; }
    public string? ErrorMessage { get; set; }

    public static SaveWorkflowSpecResult Succeeded(Guid specId, int specVersion)
        => new() { Success = true, SpecId = specId, SpecVersion = specVersion };

    public static SaveWorkflowSpecResult Failed(string error)
        => new() { Success = false, ErrorMessage = error };
}
