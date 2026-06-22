using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Domain.Interfaces;

namespace FCG_CATALOG_API.Application.Services;

public class AcquisitionService : IAcquisitionService
{
    private readonly IAcquisitionRepository _acquisitionRepository;

    public AcquisitionService(IAcquisitionRepository acquisitionRepository)
    {
        _acquisitionRepository = acquisitionRepository;
    }

    public async Task AddToLibraryAsync(Guid userId, Guid gameId, decimal pricePaid)
    {
        var existing = await _acquisitionRepository.GetAllAsync();
        if (existing.Any(a => a.UserId == userId && a.GameId == gameId))
            return;

        var acquisition = new Acquisition
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            PricePaid = pricePaid,
            AcquisitionDate = DateTime.UtcNow
        };
        await _acquisitionRepository.AddAsync(acquisition);
    }
}
