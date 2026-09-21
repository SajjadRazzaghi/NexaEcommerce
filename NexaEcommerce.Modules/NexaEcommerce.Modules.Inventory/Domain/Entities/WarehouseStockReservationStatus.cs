namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public enum WarehouseStockReservationStatus
{
    Pending = 1,
    Committed = 2,
    Released = 3,
    Expired = 4,
}