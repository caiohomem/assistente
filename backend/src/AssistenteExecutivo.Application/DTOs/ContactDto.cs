namespace AssistenteExecutivo.Application.DTOs;

public class ContactDto
{
    public Guid ContactId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Company { get; set; }
    public List<string> Emails { get; set; } = new();
    public List<string> Phones { get; set; } = new();
    public AddressDto? Address { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<RelationshipDto> Relationships { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AddressDto
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
}

public class RelationshipDto
{
    public Guid RelationshipId { get; set; }
    public Guid SourceContactId { get; set; }
    public Guid TargetContactId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? RelationshipTypeId { get; set; }
    public string? Description { get; set; }
    public float Strength { get; set; }
    public bool IsConfirmed { get; set; }
}













