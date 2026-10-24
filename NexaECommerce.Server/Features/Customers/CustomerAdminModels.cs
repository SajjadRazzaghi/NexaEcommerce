namespace NexaECommerce.Server.Features.Customers;

public sealed record CustomerAdminDto(
    string Id,
    string? DisplayName,
    string? Email,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool PhoneNumberConfirmed,
    DateTimeOffset CreatedAt,
    int TotalOrders,
    int PaidOrders,
    decimal TotalSpent,
    DateTime? LastOrderAt);