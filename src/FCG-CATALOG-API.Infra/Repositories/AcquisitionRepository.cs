using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Domain.Interfaces;

namespace FCG_CATALOG_API.Infra.Repositories;

public class AcquisitionRepository : RepositoryBase<Acquisition>, IAcquisitionRepository
{
    private readonly AppDbContext _context;

    public AcquisitionRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }
}
