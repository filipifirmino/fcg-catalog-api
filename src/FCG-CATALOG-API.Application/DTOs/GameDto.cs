namespace FCG_CATALOG_API.Application.DTOs;

public class GameDto
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Genre { get; set; }
    public string? Slug { get; set; }
    public bool? IsActive { get; set; }
}
