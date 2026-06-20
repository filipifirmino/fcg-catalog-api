using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Domain.Interfaces;

namespace FCG_CATALOG_API.Infra.Repositories;

public class GameRepository : RepositoryBase<Game>, IGameRepository
{
    private readonly AppDbContext _context;

    public GameRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }
}
