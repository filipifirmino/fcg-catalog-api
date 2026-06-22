namespace FCG_CATALOG_API.Application.DTOs;

public class FiltersDto
{
    public string? Title { get; private set; }
    public string? Genre { get; private set; }
    public decimal? Price { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public int? Page { get; private set; }
    public int? PageSize { get; private set; }
}
