using MediatR;

namespace AssistenteExecutivo.Application.Queries.Automation;

public class ListLetterheadsQuery : IRequest<ListLetterheadsResultDto>
{
    public Guid OwnerUserId { get; set; }
    public bool? ActiveOnly { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ListLetterheadsResultDto
{
    public List<LetterheadDto> Letterheads { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class LetterheadDto
{
    public Guid LetterheadId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DesignData { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}





