using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Customers.Domain.Entities;

public sealed class CustomerAddress : BaseEntity
{
    private CustomerAddress()
    {
    }

    private CustomerAddress(
        string tenantId,
        string userId,
        string title,
        string recipientName,
        string phoneNumber,
        string country,
        string province,
        string city,
        string addressLine,
        string? postalCode,
        bool isDefault)
    {
        TenantId = NormalizeRequired(
            tenantId,
            nameof(tenantId));

        UserId = NormalizeRequired(
            userId,
            nameof(userId));

        Title = NormalizeRequired(
            title,
            nameof(title));

        RecipientName = NormalizeRequired(
            recipientName,
            nameof(recipientName));

        PhoneNumber = NormalizeRequired(
            phoneNumber,
            nameof(phoneNumber));

        Country = NormalizeRequired(
            country,
            nameof(country));

        Province = NormalizeRequired(
            province,
            nameof(province));

        City = NormalizeRequired(
            city,
            nameof(city));

        AddressLine = NormalizeRequired(
            addressLine,
            nameof(addressLine));

        PostalCode =
            string.IsNullOrWhiteSpace(postalCode)
                ? null
                : postalCode.Trim();

        IsDefault = isDefault;
    }

    public string TenantId { get; private set; } = null!;

    public string UserId { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string RecipientName { get; private set; } = null!;

    public string PhoneNumber { get; private set; } = null!;

    public string Country { get; private set; } = null!;

    public string Province { get; private set; } = null!;

    public string City { get; private set; } = null!;

    public string AddressLine { get; private set; } = null!;

    public string? PostalCode { get; private set; }

    public bool IsDefault { get; private set; }

    public static CustomerAddress Create(
        string tenantId,
        string userId,
        string title,
        string recipientName,
        string phoneNumber,
        string country,
        string province,
        string city,
        string addressLine,
        string? postalCode,
        bool isDefault = false)
    {
        return new CustomerAddress(
            tenantId,
            userId,
            title,
            recipientName,
            phoneNumber,
            country,
            province,
            city,
            addressLine,
            postalCode,
            isDefault);
    }

    public void Update(
        string title,
        string recipientName,
        string phoneNumber,
        string country,
        string province,
        string city,
        string addressLine,
        string? postalCode)
    {
        Title = NormalizeRequired(
            title,
            nameof(title));

        RecipientName = NormalizeRequired(
            recipientName,
            nameof(recipientName));

        PhoneNumber = NormalizeRequired(
            phoneNumber,
            nameof(phoneNumber));

        Country = NormalizeRequired(
            country,
            nameof(country));

        Province = NormalizeRequired(
            province,
            nameof(province));

        City = NormalizeRequired(
            city,
            nameof(city));

        AddressLine = NormalizeRequired(
            addressLine,
            nameof(addressLine));

        PostalCode =
            string.IsNullOrWhiteSpace(postalCode)
                ? null
                : postalCode.Trim();
    }

    public void SetDefault()
    {
        IsDefault = true;
    }

    public void ClearDefault()
    {
        IsDefault = false;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        return value.Trim();
    }
}

