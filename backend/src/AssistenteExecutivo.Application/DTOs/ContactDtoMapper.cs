using AssistenteExecutivo.Domain.Entities;
namespace AssistenteExecutivo.Application.DTOs;

public static class ContactDtoMapper
{
    public static ContactDto MapToDto(Contact contact)
    {
        return new ContactDto
        {
            ContactId = contact.ContactId,
            OwnerUserId = contact.OwnerUserId,
            FirstName = contact.Name.FirstName,
            LastName = contact.Name.LastName,
            FullName = contact.Name.FullName,
            JobTitle = contact.JobTitle,
            Company = contact.Company,
            Emails = contact.Emails.Select(e => e.Value).ToList(),
            Phones = contact.Phones.Select(p => p.FormattedNumber).ToList(),
            Address = contact.Address != null ? new AddressDto
            {
                Street = contact.Address.Street,
                City = contact.Address.City,
                State = contact.Address.State,
                ZipCode = contact.Address.ZipCode,
                Country = contact.Address.Country
            } : null,
            Tags = contact.Tags.Select(t => t.Value).ToList(),
            Relationships = contact.Relationships.Select(r => new RelationshipDto
            {
                RelationshipId = r.RelationshipId,
                SourceContactId = r.SourceContactId,
                TargetContactId = r.TargetContactId,
                Type = r.Type,
                RelationshipTypeId = r.RelationshipTypeId,
                Description = r.Description,
                Strength = r.Strength,
                IsConfirmed = r.IsConfirmed
            }).ToList(),
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }
}

