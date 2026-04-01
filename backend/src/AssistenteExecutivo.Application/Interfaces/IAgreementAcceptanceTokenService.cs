namespace AssistenteExecutivo.Application.Interfaces;

public record AgreementAcceptanceTokenPayload(
    Guid AgreementId,
    Guid PartyId,
    Guid OwnerUserId,
    int MaxDays,
    DateTimeOffset ExpiresAt);

public interface IAgreementAcceptanceTokenService
{
    string CreateToken(Guid agreementId, Guid partyId, Guid ownerUserId, int maxDays, DateTimeOffset expiresAt);
    bool TryValidateToken(string token, out AgreementAcceptanceTokenPayload payload);
}
