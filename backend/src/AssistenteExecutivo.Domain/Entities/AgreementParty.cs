using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Entities;

public class AgreementParty
{
    private AgreementParty() { } // EF Core

    private AgreementParty(
        Guid partyId,
        Guid? contactId,
        Guid? companyId,
        string partyName,
        string? email,
        Percentage splitPercentage,
        PartyRole role,
        string? stripeAccountId,
        IClock clock)
    {
        if (partyId == Guid.Empty)
            throw new DomainException("Domain:PartyIdObrigatorio");

        if (string.IsNullOrWhiteSpace(partyName))
            throw new DomainException("Domain:PartyNameObrigatorio");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Domain:PartyEmailObrigatorio");

        if (splitPercentage == null)
            throw new DomainException("Domain:SplitPercentageObrigatorio");

        PartyId = partyId;
        ContactId = contactId;
        CompanyId = companyId;
        PartyName = partyName.Trim();
        Email = email.Trim();
        SplitPercentage = splitPercentage;
        Role = role;
        StripeAccountId = NormalizeStripeAccountId(stripeAccountId);
        CreatedAt = clock.UtcNow;
        HasAccepted = false;
    }

    public Guid PartyId { get; private set; }
    public Guid AgreementId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string PartyName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public Percentage SplitPercentage { get; private set; } = null!;
    public PartyRole Role { get; private set; }
    public string? StripeAccountId { get; private set; }
    public DateTime? StripeConnectedAt { get; private set; }
    public bool HasAccepted { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static AgreementParty Create(
        Guid partyId,
        Guid? contactId,
        Guid? companyId,
        string partyName,
        string? email,
        Percentage splitPercentage,
        PartyRole role,
        string? stripeAccountId,
        IClock clock)
    {
        return new AgreementParty(
            partyId,
            contactId,
            companyId,
            partyName,
            email,
            splitPercentage,
            role,
            stripeAccountId,
            clock);
    }

    internal void Accept(IClock clock)
    {
        if (HasAccepted)
            return;

        HasAccepted = true;
        AcceptedAt = clock.UtcNow;
    }

    internal void UpdateSplit(Percentage split)
    {
        if (split == null)
            throw new DomainException("Domain:SplitPercentageObrigatorio");

        SplitPercentage = split;
    }

    internal void UpdateDetails(
        string? partyName,
        string? email,
        Guid? contactId,
        Guid? companyId)
    {
        if (!string.IsNullOrWhiteSpace(partyName))
            PartyName = partyName.Trim();

        if (email != null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Domain:PartyEmailObrigatorio");

            Email = email.Trim();
        }

        ContactId = contactId;
        CompanyId = companyId;
    }

    internal void UpdateStripeAccountId(string? stripeAccountId)
    {
        StripeAccountId = NormalizeStripeAccountId(stripeAccountId);
    }

    internal void ConnectStripeAccount(string accountId, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new DomainException("Domain:ContaStripeInvalida");

        StripeAccountId = accountId.Trim();
        StripeConnectedAt = clock.UtcNow;
    }

    internal void DisconnectStripeAccount()
    {
        StripeAccountId = null;
        StripeConnectedAt = null;
    }

    private static string? NormalizeStripeAccountId(string? stripeAccountId)
    {
        if (string.IsNullOrWhiteSpace(stripeAccountId))
            return null;

        return stripeAccountId.Trim();
    }

    internal void SetAgreementId(Guid agreementId)
    {
        if (agreementId == Guid.Empty)
            throw new DomainException("Domain:AgreementIdObrigatorio");

        AgreementId = agreementId;
    }
}
