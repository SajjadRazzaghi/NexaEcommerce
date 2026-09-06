using NexaEcommerce.Modules.Catalog.Application.CatalogAttributes.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class CatalogAttributeService : ICatalogAttributeService
{
    private readonly ICatalogAttributeRepository _repository;

    public CatalogAttributeService(ICatalogAttributeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<CatalogAttributeDto>> GetAllAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var attributes = string.IsNullOrWhiteSpace(search)
            ? await _repository.GetAllAsync(cancellationToken)
            : await _repository.SearchAsync(search, cancellationToken);

        return attributes
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(Map)
            .ToArray();
    }

    public async Task<CatalogAttributeDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var attribute = await _repository.GetByIdAsync(id, cancellationToken);

        return attribute is null
            ? null
            : Map(attribute);
    }

    public async Task<CatalogAttributeDto> CreateAsync(
        CreateCatalogAttributeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var name = NormalizeRequired(dto.Name, nameof(dto.Name));
        var code = NormalizeCode(dto.Code, nameof(dto.Code));

        var existing = await _repository.GetByCodeAsync(code, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException(
                $"A catalog attribute with code '{code}' already exists.");

        var attribute = new CatalogAttribute(
            name,
            code,
            NormalizeOptional(dto.Description),
            NormalizeOptional(dto.DisplayType),
            dto.IsRequired,
            dto.IsFilterable,
            dto.IsVariantAttribute,
            dto.IsActive,
            dto.DisplayOrder);

        await _repository.AddAsync(attribute, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(attribute);
    }

    public async Task<CatalogAttributeDto?> UpdateAsync(
        Guid id,
        UpdateCatalogAttributeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var attribute = await _repository.GetByIdAsync(id, cancellationToken);

        if (attribute is null)
            return null;

        var name = NormalizeRequired(dto.Name, nameof(dto.Name));
        var code = NormalizeCode(dto.Code, nameof(dto.Code));

        var existing = await _repository.GetByCodeAsync(code, cancellationToken);

        if (existing is not null && existing.Id != id)
            throw new InvalidOperationException(
                $"A catalog attribute with code '{code}' already exists.");

        attribute.Update(
            name,
            code,
            NormalizeOptional(dto.Description),
            NormalizeOptional(dto.DisplayType),
            dto.IsRequired,
            dto.IsFilterable,
            dto.IsVariantAttribute,
            dto.IsActive,
            dto.DisplayOrder);

        _repository.Update(attribute);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(attribute);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var attribute = await _repository.GetByIdAsync(id, cancellationToken);

        if (attribute is null)
            return false;

        _repository.Remove(attribute);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<CatalogAttributeValueDto?> AddValueAsync(
        Guid attributeId,
        CreateCatalogAttributeValueDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var attribute = await _repository.GetByIdAsync(
            attributeId,
            cancellationToken);

        if (attribute is null)
            return null;

        var value = NormalizeRequired(dto.Value, nameof(dto.Value));
        var displayValue = NormalizeOptional(dto.DisplayValue);
        var colorHex = NormalizeHex(dto.ColorHex);

        var existing = attribute.Values.FirstOrDefault(x =>
            string.Equals(
                x.Value.Trim(),
                value,
                StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            throw new InvalidOperationException(
                $"The value '{value}' already exists for attribute '{attribute.Name}'.");

        var attributeValue = attribute.AddValue(
            value,
            displayValue,
            colorHex,
            dto.DisplayOrder,
            dto.IsActive);

        _repository.Update(attribute);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(attributeValue);
    }

    public async Task<CatalogAttributeValueDto?> UpdateValueAsync(
        Guid attributeId,
        Guid valueId,
        UpdateCatalogAttributeValueDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var attribute = await _repository.GetByIdAsync(
            attributeId,
            cancellationToken);

        if (attribute is null)
            return null;

        var attributeValue = attribute.Values.FirstOrDefault(
            x => x.Id == valueId);

        if (attributeValue is null)
            return null;

        var value = NormalizeRequired(dto.Value, nameof(dto.Value));
        var displayValue = NormalizeOptional(dto.DisplayValue);
        var colorHex = NormalizeHex(dto.ColorHex);

        var duplicate = attribute.Values.FirstOrDefault(x =>
            x.Id != valueId &&
            string.Equals(
                x.Value.Trim(),
                value,
                StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null)
            throw new InvalidOperationException(
                $"The value '{value}' already exists for attribute '{attribute.Name}'.");

        attributeValue.Update(
            value,
            displayValue,
            colorHex,
            dto.DisplayOrder,
            dto.IsActive);

        _repository.Update(attribute);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(attributeValue);
    }

    public async Task<bool> DeleteValueAsync(
        Guid attributeId,
        Guid valueId,
        CancellationToken cancellationToken = default)
    {
        var attribute = await _repository.GetByIdAsync(
            attributeId,
            cancellationToken);

        if (attribute is null)
            return false;

        var attributeValue = attribute.Values.FirstOrDefault(
            x => x.Id == valueId);

        if (attributeValue is null)
            return false;

        attribute.Values.Remove(attributeValue);

        _repository.Update(attribute);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static CatalogAttributeDto Map(CatalogAttribute attribute)
    {
        return new CatalogAttributeDto(
            attribute.Id,
            attribute.Name,
            attribute.Code,
            attribute.Description,
            attribute.DisplayType,
            attribute.IsRequired,
            attribute.IsFilterable,
            attribute.IsVariantAttribute,
            attribute.IsActive,
            attribute.DisplayOrder,
            attribute.Values
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Value)
                .Select(Map)
                .ToArray());
    }

    private static CatalogAttributeValueDto Map(
        CatalogAttributeValue value)
    {
        return new CatalogAttributeValueDto(
            value.Id,
            value.CatalogAttributeId,
            value.Value,
            value.DisplayValue,
            value.ColorHex,
            value.DisplayOrder,
            value.IsActive);
    }

    private static string NormalizeRequired(
        string? value,
        string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException(
                "Value cannot be empty.",
                parameterName);

        return normalized;
    }

    private static string NormalizeCode(
        string? value,
        string parameterName)
    {
        var normalized = NormalizeRequired(value, parameterName);

        return normalized
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeHex(string? value)
    {
        var normalized = NormalizeOptional(value);

        if (normalized is null)
            return null;

        normalized = normalized.StartsWith('#')
            ? normalized
            : $"#{normalized}";

        if (normalized.Length != 7)
            throw new ArgumentException(
                "ColorHex must be a valid 6-digit hexadecimal color.");

        for (var i = 1; i < normalized.Length; i++)
        {
            if (!Uri.IsHexDigit(normalized[i]))
                throw new ArgumentException(
                    "ColorHex must be a valid 6-digit hexadecimal color.");
        }

        return normalized.ToUpperInvariant();
    }
}

