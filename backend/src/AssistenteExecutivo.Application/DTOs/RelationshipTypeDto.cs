namespace AssistenteExecutivo.Application.DTOs;

public class RelationshipTypeDto
{
    public Guid RelationshipTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
