namespace AssistenteExecutivo.Domain.Constants;

public static class RelationshipTypeDefaults
{
    public static readonly IReadOnlyList<string> Names = new[]
    {
        "Cliente",
        "Parceiro",
        "Fornecedor",
        "Investidor",
        "Colega",
        "Amigo",
        "Família",
        "Influenciador",
        "Prospect"
    };
}
