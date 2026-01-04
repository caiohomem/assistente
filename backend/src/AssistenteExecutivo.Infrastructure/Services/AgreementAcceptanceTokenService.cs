using AssistenteExecutivo.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace AssistenteExecutivo.Infrastructure.Services;

public class AgreementAcceptanceTokenService : IAgreementAcceptanceTokenService
{
    private readonly IDataProtector _protector;

    public AgreementAcceptanceTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("agreement-acceptance-token");
    }

    public string CreateToken(Guid agreementId, Guid partyId, Guid ownerUserId, int maxDays, DateTimeOffset expiresAt)
    {
        var payload = new AgreementAcceptanceTokenPayload(agreementId, partyId, ownerUserId, maxDays, expiresAt);
        var json = JsonSerializer.Serialize(payload);
        return _protector.Protect(json);
    }

    public bool TryValidateToken(string token, out AgreementAcceptanceTokenPayload payload)
    {
        payload = default;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var json = _protector.Unprotect(token);
            var parsed = JsonSerializer.Deserialize<AgreementAcceptanceTokenPayload>(json);
            if (parsed == null)
            {
                return false;
            }

            if (parsed.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return false;
            }

            payload = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
