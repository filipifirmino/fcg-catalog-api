namespace FCG_CATALOG_API.Domain.Interfaces;

public interface IAcquisitionService
{
    Task AddToLibraryAsync(Guid userId, Guid gameId, decimal pricePaid);
}
