using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Interfaces;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Contact>> GetAllAsync(Guid ownerUserId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<Contact?> GetByEmailAsync(string email, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Contact?> GetByPhoneAsync(string phone, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Contact contact, CancellationToken cancellationToken = default);
    Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default);
    Task DeleteAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid contactId, Guid ownerUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica se um contato existe (sem filtro de owner), útil para diagnóstico.
    /// Retorna informações sobre o status do contato.
    /// </summary>
    Task<(bool Exists, Guid? OwnerUserId, bool IsDeleted)> GetContactStatusAsync(Guid contactId, CancellationToken cancellationToken = default);
}

