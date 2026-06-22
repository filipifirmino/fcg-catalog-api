namespace FCG_CATALOG_API.Domain.Entities;

public class Game
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public string Genre { get; private set; }
    public string Slug { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Game() { }

    public Game(string title, string description, decimal price, string genre)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new Exception("Título é obrigatório");
        if (price < 0)
            throw new Exception("Preço não pode ser negativo");
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Price = price;
        Genre = genre;
        Slug = GenerateSlug(title);
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? title, string? description, decimal? price, string? genre, bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
            Slug = GenerateSlug(title);
        }
        if (description != null) Description = description;
        if (price.HasValue)
        {
            if (price.Value < 0) throw new Exception("Preço não pode ser negativo");
            Price = price.Value;
        }
        if (genre != null) Genre = genre;
        if (isActive.HasValue) IsActive = isActive.Value;
    }

    private string GenerateSlug(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;

        string slug = title.ToLowerInvariant();

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").Trim('-');

        return slug;
    }
}
