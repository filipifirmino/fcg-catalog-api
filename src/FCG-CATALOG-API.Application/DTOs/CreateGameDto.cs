namespace FCG_CATALOG_API.Application.DTOs;

public class CreateGameDto
{
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public decimal? Price { get; private set; }
    public string? Genre { get; private set; }
    public string? Slug { get; private set; }
    public bool? IsActive { get; private set; }
}
