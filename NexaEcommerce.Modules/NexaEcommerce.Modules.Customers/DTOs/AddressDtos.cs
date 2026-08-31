namespace NexaEcommerce.Modules.Customers.Application.DTOs;

public sealed record CreateAddressRequest(
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Country,
    string Province,
    string City,
    string AddressLine,
    string? PostalCode,
    bool IsDefault = false);

public sealed record UpdateAddressRequest(
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Country,
    string Province,
    string City,
    string AddressLine,
    string? PostalCode);

public sealed record CustomerAddressDto(
    Guid Id,
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Country,
    string Province,
    string City,
    string AddressLine,
    string? PostalCode,
    bool IsDefault);
