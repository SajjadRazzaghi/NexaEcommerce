/*
    NexaECommerce - Convert ProductVariant barcodes to 12 numeric digits.

    The old generator stored: NX + 12 random digits.
    This migration removes only the NX prefix and retains the original
    12-digit suffix, so the same ProductVariant keeps its barcode identity.

    Run once against the Nexa database after taking a database backup.
    Safe to run again after successful conversion.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ConvertedRows int = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Catalog].[ProductVariants]', N'U') IS NULL
    BEGIN
        ;THROW 51000, N'Table [Catalog].[ProductVariants] was not found.', 1;
    END;

    -- Refuse unexpected formats rather than silently changing unknown barcodes.
    IF EXISTS
    (
        SELECT 1
        FROM [Catalog].[ProductVariants]
        WHERE [Barcode] IS NOT NULL
          AND NOT
          (
              (
                  DATALENGTH([Barcode]) = 28
                  AND LEFT([Barcode], 2) COLLATE Latin1_General_100_BIN2 = N'NX'
                  AND SUBSTRING([Barcode], 3, 12) COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9]%'
              )
              OR
              (
                  DATALENGTH([Barcode]) = 24
                  AND [Barcode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9]%'
              )
          )
    )
    BEGIN
        ;THROW 51001, N'Unexpected non-numeric barcode format found. No barcode was changed; inspect Catalog.ProductVariants.Barcode first.', 1;
    END;

    -- The numeric suffix must not already belong to another variant.
    IF EXISTS
    (
        SELECT 1
        FROM [Catalog].[ProductVariants] AS legacy
        INNER JOIN [Catalog].[ProductVariants] AS numericBarcode
            ON numericBarcode.[Barcode] = SUBSTRING(legacy.[Barcode], 3, 12)
           AND numericBarcode.[Id] <> legacy.[Id]
        WHERE DATALENGTH(legacy.[Barcode]) = 28
          AND LEFT(legacy.[Barcode], 2) COLLATE Latin1_General_100_BIN2 = N'NX'
    )
    BEGIN
        ;THROW 51002, N'Barcode conversion would create a duplicate numeric barcode. No barcode was changed; inspect the conflicting variants first.', 1;
    END;

    UPDATE [Catalog].[ProductVariants]
       SET [Barcode] = SUBSTRING([Barcode], 3, 12)
     WHERE DATALENGTH([Barcode]) = 28
       AND LEFT([Barcode], 2) COLLATE Latin1_General_100_BIN2 = N'NX'
       AND SUBSTRING([Barcode], 3, 12) COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9]%';

    SET @ConvertedRows = @@ROWCOUNT;

    -- Final invariant: every non-null stored barcode is exactly 12 ASCII digits.
    IF EXISTS
    (
        SELECT 1
        FROM [Catalog].[ProductVariants]
        WHERE [Barcode] IS NOT NULL
          AND
          (
              DATALENGTH([Barcode]) <> 24
              OR [Barcode] COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
          )
    )
    BEGIN
        ;THROW 51003, N'Numeric barcode validation failed. The transaction will be rolled back.', 1;
    END;

    COMMIT TRANSACTION;

    SELECT
        @ConvertedRows AS ConvertedVariantBarcodes,
        COUNT_BIG(*) AS TotalNumericVariantBarcodes
    FROM [Catalog].[ProductVariants]
    WHERE [Barcode] IS NOT NULL;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
