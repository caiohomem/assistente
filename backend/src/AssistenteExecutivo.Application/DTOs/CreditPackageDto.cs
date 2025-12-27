namespace AssistenteExecutivo.Application.DTOs;

public class CreditPackageDto
{
    public Guid PackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; } // -1 para ilimitado
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}




