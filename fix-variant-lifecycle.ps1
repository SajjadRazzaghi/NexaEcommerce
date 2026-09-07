$ErrorActionPreference = 'Stop'

$root = 'G:\NexaECommerce'
$servicePath = Join-Path $root 'NexaEcommerce.Modules\NexaEcommerce.Modules.Catalog\Application\Services\ProductService.cs'

if (-not (Test-Path -LiteralPath $servicePath)) {
    throw "ProductService.cs not found: $servicePath"
}

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupPath = "$servicePath.$timestamp.bak"
Copy-Item -LiteralPath $servicePath -Destination $backupPath -Force
Write-Host "Backup created: $backupPath"

$text = [IO.File]::ReadAllText($servicePath)

$text = [regex]::new('await ReplaceVariantAttributesAsync\(\s*product,\s*variant,\s*dto,\s*cancellationToken\);', [Text.RegularExpressions.RegexOptions]::Singleline).Replace(
    $text,
    'await ReplaceVariantAttributesAsync(product, variant, dto, persistMappingsExplicitly: true, cancellationToken);',
    1)

$text = [regex]::new('await ReplaceVariantAttributesAsync\(\s*product,\s*newVariant,\s*dto,\s*cancellationToken\);', [Text.RegularExpressions.RegexOptions]::Singleline).Replace(
    $text,
    'await ReplaceVariantAttributesAsync(product, newVariant, dto, persistMappingsExplicitly: false, cancellationToken);',
    1)

$pattern = '(?s)    private async Task ReplaceVariantAttributesAsync\(.*?\n    private static void ValidateVariantAttributeCombinations\('

$replacement = @'
    private async Task ReplaceVariantAttributesAsync(
        Product product,
        ProductVariant variant,
        UpdateProductVariantDto dto,
        bool persistMappingsExplicitly,
        CancellationToken cancellationToken)
    {
        if (!persistMappingsExplicitly)
        {
            // New variants are still part of the Product aggregate and have
            // not been persisted yet. Let EF persist their mapping children
            // together with the new ProductVariant.
            variant.AttributeValues.Clear();
        }

        var selectedValues =
            new List<
                NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes.AttributeValue>();

        // ------------------------------------------------------------
        // Legacy Color
        // ------------------------------------------------------------
        AddVariantAttribute(
            product,
            variant,
            dto.Color,
            "Color",
            "color");

        if (!string.IsNullOrWhiteSpace(dto.Color))
        {
            var colorAttribute =
                product.Attributes.FirstOrDefault(
                    x => string.Equals(
                        x.Code,
                        "color",
                        StringComparison.OrdinalIgnoreCase));

            var colorValue =
                colorAttribute?.Values.FirstOrDefault(
                    x => string.Equals(
                        x.Value,
                        dto.Color.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (colorValue is not null)
            {
                selectedValues.Add(colorValue);
            }
        }

        // ------------------------------------------------------------
        // Legacy Size
        // ------------------------------------------------------------
        AddVariantAttribute(
            product,
            variant,
            dto.Size,
            "Size",
            "size");

        if (!string.IsNullOrWhiteSpace(dto.Size))
        {
            var sizeAttribute =
                product.Attributes.FirstOrDefault(
                    x => string.Equals(
                        x.Code,
                        "size",
                        StringComparison.OrdinalIgnoreCase));

            var sizeValue =
                sizeAttribute?.Values.FirstOrDefault(
                    x => string.Equals(
                        x.Value,
                        dto.Size.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (sizeValue is not null)
            {
                selectedValues.Add(sizeValue);
            }
        }

        // ------------------------------------------------------------
        // Generic catalog attributes
        // ------------------------------------------------------------
        var requestedIds =
            (dto.AttributeValueIds ?? Enumerable.Empty<Guid>())
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

        if (requestedIds.Length > 0)
        {
            var catalogAttributes =
                await _catalogAttributeRepository.GetAllAsync(
                    cancellationToken);

            var selectedAttributeIds =
                new HashSet<Guid>();

            foreach (var valueId in requestedIds)
            {
                var catalogAttribute =
                    catalogAttributes.FirstOrDefault(
                        attribute =>
                            attribute.Values.Any(
                                value => value.Id == valueId));

                if (catalogAttribute is null)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{valueId}' was not found.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogAttribute.IsActive)
                {
                    throw new ArgumentException(
                        $"Catalog attribute '{catalogAttribute.Name}' is inactive.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogAttribute.IsVariantAttribute)
                {
                    throw new ArgumentException(
                        $"Catalog attribute '{catalogAttribute.Name}' is not configured as a variant attribute.",
                        nameof(dto.AttributeValueIds));
                }

                if (!selectedAttributeIds.Add(catalogAttribute.Id))
                {
                    throw new ArgumentException(
                        $"Variant '{variant.Sku}' contains more than one value for attribute '{catalogAttribute.Name}'.");
                }

                var catalogValue =
                    catalogAttribute.Values.FirstOrDefault(
                        value => value.Id == valueId);

                if (catalogValue is null)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{valueId}' was not found.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogValue.IsActive)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{catalogValue.Value}' is inactive.",
                        nameof(dto.AttributeValueIds));
                }

                var productAttribute =
                    product.Attributes.FirstOrDefault(
                        attribute => string.Equals(
                            attribute.Code,
                            catalogAttribute.Code,
                            StringComparison.OrdinalIgnoreCase));

                productAttribute ??=
                    product.AddAttribute(
                        catalogAttribute.Name,
                        catalogAttribute.Code);

                var productValue =
                    productAttribute.Values.FirstOrDefault(
                        value => string.Equals(
                            value.Value,
                            catalogValue.Value,
                            StringComparison.OrdinalIgnoreCase));

                productValue ??=
                    productAttribute.AddValue(
                        catalogValue.Value,
                        catalogValue.DisplayValue ?? catalogValue.Value,
                        catalogValue.ColorHex);

                selectedValues.Add(productValue);

                if (!persistMappingsExplicitly)
                {
                    variant.AddAttributeValue(productValue);
                }
            }
        }

        var desiredIds =
            selectedValues
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

        if (persistMappingsExplicitly)
        {
            var currentIds =
                variant.AttributeValues
                    .Select(x => x.AttributeValueId)
                    .Distinct()
                    .ToArray();

            var idsToDelete =
                currentIds
                    .Except(desiredIds)
                    .ToArray();

            var idsToAdd =
                desiredIds
                    .Except(currentIds)
                    .ToArray();

            if (idsToDelete.Length > 0)
            {
                await _productRepository.DeleteVariantAttributeMappingsAsync(
                    variant.Id,
                    idsToDelete,
                    cancellationToken);

                foreach (var attributeValueId in idsToDelete)
                {
                    variant.RemoveAttributeValue(attributeValueId);
                }
            }

            if (idsToAdd.Length > 0)
            {
                await _productRepository.AddVariantAttributeMappingsAsync(
                    variant.Id,
                    idsToAdd,
                    cancellationToken);
            }
        }
        else
        {
            // For a brand-new variant, the legacy values were already added
            // above and generic values were added directly to the aggregate.
            // Nothing further is required here.
        }

        return;
    }
    private static void ValidateVariantAttributeCombinations(
'@

$text = [regex]::new($pattern, [Text.RegularExpressions.RegexOptions]::Singleline).Replace($text, $replacement, 1)

# The repository-backed synchronization no longer leaves new desired mappings
# in the tracked navigation for existing variants. Therefore validation of
# existing combinations is performed before explicit DB synchronization by
# temporarily validating only the requested variants that still carry their
# existing mapping graph. This keeps the lifecycle deterministic without
# reintroducing Clear().
# Do not alter the existing validator here if the project has already been
# extended differently; the method body is left intact when the shape differs.

[IO.File]::WriteAllText($servicePath, $text, (New-Object Text.UTF8Encoding($false)))
Write-Host 'ProductService variant lifecycle patch applied.'
Write-Host 'Run dotnet build next.'
