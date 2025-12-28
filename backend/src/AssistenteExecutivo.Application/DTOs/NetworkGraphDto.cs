namespace AssistenteExecutivo.Application.DTOs;

public record NetworkGraphDto
{
    public List<GraphNodeDto> Nodes { get; init; } = new();
    public List<GraphEdgeDto> Edges { get; init; } = new();
}

public class GraphNodeDto
{
    public Guid ContactId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? PrimaryEmail { get; set; }
}

public class GraphEdgeDto
{
    public Guid RelationshipId { get; set; }
    public Guid SourceContactId { get; set; }
    public Guid TargetContactId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public float Strength { get; set; }
    public bool IsConfirmed { get; set; }
}

