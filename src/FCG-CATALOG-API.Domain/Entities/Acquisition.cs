using System;

namespace FCG_CATALOG_API.Domain.Entities;

public class Acquisition
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal PricePaid { get; set; }
    public DateTime AcquisitionDate { get; set; }
}
