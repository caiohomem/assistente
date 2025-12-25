using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.DomainServices;

public class ContactDeduplicationService
{
    public DeduplicationDecision Decide(
        Contact existingContact,
        OcrExtract newExtract)
    {
        if (existingContact == null)
            throw new DomainException("Domain:ExistingContactObrigatorio");

        if (newExtract == null)
            throw new DomainException("Domain:OcrExtractObrigatorio");

        // Heurística 1: Email exato
        if (!string.IsNullOrWhiteSpace(newExtract.Email))
        {
            var newEmail = EmailAddress.Create(newExtract.Email);
            if (existingContact.Emails.Any(e => e == newEmail))
            {
                return new DeduplicationDecision(
                    DeduplicationAction.Merge,
                    existingContact.ContactId,
                    "EmailExato");
            }
        }

        // Heurística 2: Telefone exato
        if (!string.IsNullOrWhiteSpace(newExtract.Phone))
        {
            var newPhone = PhoneNumber.Create(newExtract.Phone);
            if (existingContact.Phones.Any(p => p == newPhone))
            {
                return new DeduplicationDecision(
                    DeduplicationAction.Merge,
                    existingContact.ContactId,
                    "TelefoneExato");
            }
        }

        // Heurística 3: Nome similar + mesma empresa (se disponível)
        if (!string.IsNullOrWhiteSpace(newExtract.Name) && !string.IsNullOrWhiteSpace(newExtract.Company))
        {
            var similarity = CalculateNameSimilarity(existingContact.Name.FullName, newExtract.Name);
            if (similarity > 0.8) // 80% de similaridade
            {
                return new DeduplicationDecision(
                    DeduplicationAction.Merge,
                    existingContact.ContactId,
                    "NomeSimilarEmesmaEmpresa");
            }
        }

        // Heurística 4: Nome muito similar (sem empresa)
        if (!string.IsNullOrWhiteSpace(newExtract.Name))
        {
            var similarity = CalculateNameSimilarity(existingContact.Name.FullName, newExtract.Name);
            if (similarity > 0.9) // 90% de similaridade
            {
                return new DeduplicationDecision(
                    DeduplicationAction.Merge,
                    existingContact.ContactId,
                    "NomeMuitoSimilar");
            }
        }

        // Não é duplicado, criar novo
        return new DeduplicationDecision(
            DeduplicationAction.Create,
            null,
            "NaoDuplicado");
    }

    private double CalculateNameSimilarity(string name1, string name2)
    {
        // Implementação simples de similaridade (Levenshtein normalizado)
        // Em produção, considerar usar biblioteca como FuzzySharp
        var maxLength = Math.Max(name1.Length, name2.Length);
        if (maxLength == 0) return 1.0;

        var distance = LevenshteinDistance(name1.ToLowerInvariant(), name2.ToLowerInvariant());
        return 1.0 - (double)distance / maxLength;
    }

    private int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}

public enum DeduplicationAction
{
    Create,
    Merge
}

public class DeduplicationDecision
{
    public DeduplicationAction Action { get; }
    public Guid? ExistingContactId { get; }
    public string Reason { get; }

    public DeduplicationDecision(DeduplicationAction action, Guid? existingContactId, string reason)
    {
        Action = action;
        ExistingContactId = existingContactId;
        Reason = reason;
    }
}

