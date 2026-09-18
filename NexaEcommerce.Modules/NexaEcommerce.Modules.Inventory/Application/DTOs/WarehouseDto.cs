namespace NexaEcommerce.Modules.Inventory.Application.DTOs;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? AddressLine,
    string? City,
    string? PostalCode,
    string? Phone,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record WarehouseLocationDto(
    Guid Id,
    Guid WarehouseId,
    string Code,
    string Name,
    string? Zone,
    string? Rack,
    string? Shelf,
    string? Bin,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateWarehouseRequest(
    string Code,
    string Name,
    string? AddressLine = null,
    string? City = null,
    string? PostalCode = null,
    string? Phone = null,
    bool IsDefault = false);

public sealed record UpdateWarehouseRequest(
    string Code,
    string Name,
    string? AddressLine = null,
    string? City = null,
    string? PostalCode = null,
    string? Phone = null);

public sealed record SetWarehouseStatusRequest(
    bool IsActive);

public sealed record CreateWarehouseLocationRequest(
    Guid WarehouseId,
    string Code,
    string Name,
    string? Zone = null,
    string? Rack = null,
    string? Shelf = null,
    string? Bin = null);

public sealed record UpdateWarehouseLocationRequest(
    string Code,
    string Name,
    string? Zone = null,
    string? Rack = null,
    string? Shelf = null,
    string? Bin = null);

public sealed record SetWarehouseLocationStatusRequest(
    bool IsActive);
