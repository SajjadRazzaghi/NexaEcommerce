BEGIN TRANSACTION;
CREATE TABLE [Orders].[PosSales] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] nvarchar(64) NOT NULL,
    [SellerUserId] nvarchar(450) NOT NULL,
    [SaleNumber] nvarchar(64) NOT NULL,
    [IdempotencyKey] nvarchar(128) NOT NULL,
    [Status] int NOT NULL,
    [PaymentMethod] int NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaymentReference] nvarchar(256) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [PaidAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PosSales] PRIMARY KEY ([Id])
);

CREATE TABLE [Orders].[PosSaleItems] (
    [Id] uniqueidentifier NOT NULL,
    [PosSaleId] uniqueidentifier NOT NULL,
    [ProductVariantId] uniqueidentifier NOT NULL,
    [Barcode] nvarchar(128) NULL,
    [Sku] nvarchar(100) NOT NULL,
    [ProductName] nvarchar(250) NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    [ReservationKey] nvarchar(128) NOT NULL,
    [AttributesJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PosSaleItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PosSaleItems_PosSales_PosSaleId] FOREIGN KEY ([PosSaleId]) REFERENCES [Orders].[PosSales] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_PosSaleItems_PosSaleId] ON [Orders].[PosSaleItems] ([PosSaleId]);

CREATE INDEX [IX_PosSaleItems_ProductVariantId] ON [Orders].[PosSaleItems] ([ProductVariantId]);

CREATE UNIQUE INDEX [IX_PosSales_TenantId_SaleNumber] ON [Orders].[PosSales] ([TenantId], [SaleNumber]);

CREATE UNIQUE INDEX [IX_PosSales_TenantId_SellerUserId_IdempotencyKey] ON [Orders].[PosSales] ([TenantId], [SellerUserId], [IdempotencyKey]);

INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
VALUES (N'20261022181252_AddPointOfSale', N'10.0.10');

COMMIT;
GO

