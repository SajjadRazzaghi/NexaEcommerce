# NexaEcommerce — Phase 71

## Scope

This phase closes the most important contract mismatch found after the `main` branch was updated:

**Catalog owns product/variant metadata; Inventory owns sellable stock.**

It also makes the Variant editor use the same identity model on both sides:

- React sends `CatalogAttributeValue` IDs for the selected variant dimensions.
- `ProductService` resolves those IDs to the product's canonical `ProductAttribute` / `AttributeValue` records.
- `ProductAttribute.Role` is promoted to `VariantDefining` from actual variant usage instead of depending on the legacy `CatalogAttribute.IsVariantAttribute` flag.
- New variants are created with catalog stock `0`.
- The old product-level stock mutation path now fails explicitly instead of mutating `ProductVariant.StockQuantity`.
- The missing `IProductStockReader` adapter is registered so the current `ProductService` can resolve live stock from `Inventory.WarehouseStock`.
- Frontend product/variant types now expose the canonical variant metadata returned by the backend.

## Files changed

### Backend

`NexaEcommerce.Modules/NexaEcommerce.Modules.Catalog/Application/Services/ProductService.cs`

- Fixes missing constructor injection for `ICategoryRepository` and `ICatalogAttributeRepository`.
- Persists the `CatalogAttributeId` on product attributes.
- Carries catalog role/required/display-order metadata into product attributes.
- Persists `CatalogAttributeValueId` when a product attribute value is controlled by the catalog.
- Resolves Variant selections from `CatalogAttributeValue` IDs and creates/repairs the corresponding Product-side records.
- Prevents Catalog from changing stock.
- Forces new variants to start with stock `0`.

`NexaECommerce.Server/Features/Products/ProductStockReaderAdapter.cs`

- New adapter from Inventory's `IStockReader` to Catalog's `IProductStockReader` using the current tenant.

`NexaECommerce.Server/Extensions/ModuleRegistrationExtensions.cs`

- Registers the adapter after Inventory.

### Frontend

`nexaecommerce.client/src/modules/catalog/api/products.ts`

- Adds canonical ProductAttribute/VariantAttribute identifiers and variant barcode, combination and image metadata.

`nexaecommerce.client/src/modules/catalog/products/components/ProductForm.tsx`

- Stops using the legacy `CatalogAttribute.isVariantAttribute` flag to decide which attributes may be Variant dimensions.
- Any active catalog attribute with defined catalog values can now be selected as a Variant dimension.
- Edit mode no longer discards generic Variant dimensions merely because the legacy flag is false.
- Validation now requires one value for every selected Variant dimension, matching backend combination integrity.

## Apply

The supplied Python script is intentionally fail-fast. It checks that each expected source block exists exactly once before writing it.

```powershell
python .\apply_nexaecommerce_phase71.py
```

Run it from the repository root after downloading the script there.

Then run:

```powershell
git diff --check
dotnet build .\NexaECommerce.Server\NexaECommerce.Server.csproj
cd .\nexaecommerce.client
npm run build
cd ..
dotnet test .\tests\Tests.Integration\NexaEcommerce.Tests.Integration.csproj
```

If the source tree has already moved beyond the exact blocks from the inspected `main` commit, the script stops instead of silently overwriting unrelated work.

## Important limitation

The GitHub integration in this session can read the repository but returned HTTP `403` when asked to create a branch. Therefore no remote branch, commit, or PR was created by this session.

The inspected `main` head was:

`cd1096c84698630e16004fe1c03a9a8fd21cb2bb`

This phase should be applied and committed locally, then pushed to a dedicated branch such as:

`phase-71-catalog-canonicalization`

## Next large phase

The next architectural phase should complete **CategoryAttribute governance + effective Product Attribute Definitions**:

`CategoryAttribute -> effective definition for selected categories -> ProductAttribute`

That phase should add category-level attribute management endpoints/UI, merge defaults across multiple categories deterministically, expose them to the Product Editor, and remove remaining write-time reliance on the legacy `CatalogAttribute.IsVariantAttribute` flag.
