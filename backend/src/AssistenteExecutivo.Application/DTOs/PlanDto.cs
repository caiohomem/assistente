namespace AssistenteExecutivo.Application.DTOs;

public class PlanDto
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty; // "monthly" or "yearly"
    public List<string> Features { get; set; } = new();
    public PlanLimitsDto? Limits { get; set; }
    public bool IsActive { get; set; }
    public bool Highlighted { get; set; }
}

public class PlanLimitsDto
{
    public int? Contacts { get; set; }
    public int? Notes { get; set; }
    public int? CreditsPerMonth { get; set; }
    public decimal? StorageGB { get; set; }
}






