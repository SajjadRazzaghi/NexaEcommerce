IF OBJECT_ID(N'[Orders].[__EFMigrationsHistory_Orders]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Orders') IS NULL EXEC(N'CREATE SCHEMA [Orders];');
    CREATE TABLE [Orders].[__EFMigrationsHistory_Orders] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory_Orders] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827084230_InitialOrders'
)
BEGIN
    IF SCHEMA_ID(N'Orders') IS NULL EXEC(N'CREATE SCHEMA [Orders];');
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827084230_InitialOrders'
)
BEGIN
    CREATE TABLE [Orders].[Orders] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [OrderNumber] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [ShippingAmount] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [ShippingFullName] nvarchar(200) NOT NULL,
        [ShippingPhone] nvarchar(50) NOT NULL,
        [ShippingAddress] nvarchar(1000) NOT NULL,
        [ShippingCity] nvarchar(150) NOT NULL,
        [ShippingPostalCode] nvarchar(30) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827084230_InitialOrders'
)
BEGIN
    CREATE TABLE [Orders].[OrderItems] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [Sku] nvarchar(100) NOT NULL,
        [ProductName] nvarchar(300) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders].[Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827084230_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [Orders].[OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827084230_InitialOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_TenantId_OrderNumber] ON [Orders].[Orders] ([TenantId], [OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827084230_InitialOrders'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260827084230_InitialOrders', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827135413_AddOrderIdempotency'
)
BEGIN
    ALTER TABLE [Orders].[Orders] ADD [IdempotencyKey] nvarchar(128) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827135413_AddOrderIdempotency'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_TenantId_UserId_IdempotencyKey] ON [Orders].[Orders] ([TenantId], [UserId], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260827135413_AddOrderIdempotency'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260827135413_AddOrderIdempotency', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830064103_AddOrderInventoryReservations'
)
BEGIN
    CREATE TABLE [Orders].[OrderInventoryReservations] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [ReservationKey] nvarchar(256) NOT NULL,
        [ProductVariantId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [Status] int NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_OrderInventoryReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderInventoryReservations_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders].[Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830064103_AddOrderInventoryReservations'
)
BEGIN
    CREATE INDEX [IX_OrderInventoryReservations_OrderId_ProductVariantId] ON [Orders].[OrderInventoryReservations] ([OrderId], [ProductVariantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830064103_AddOrderInventoryReservations'
)
BEGIN
    CREATE INDEX [IX_OrderInventoryReservations_Status_ExpiresAt] ON [Orders].[OrderInventoryReservations] ([Status], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830064103_AddOrderInventoryReservations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrderInventoryReservations_TenantId_ReservationKey] ON [Orders].[OrderInventoryReservations] ([TenantId], [ReservationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830064103_AddOrderInventoryReservations'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260830064103_AddOrderInventoryReservations', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830172740_AddPaymentAttempts'
)
BEGIN
    CREATE TABLE [Orders].[PaymentAttempts] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [IdempotencyKey] nvarchar(128) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [Status] int NOT NULL,
        [GatewayName] nvarchar(100) NULL,
        [GatewayReference] nvarchar(200) NULL,
        [FailureCode] nvarchar(100) NULL,
        [FailureMessage] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_PaymentAttempts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830172740_AddPaymentAttempts'
)
BEGIN
    CREATE INDEX [IX_PaymentAttempts_TenantId_OrderId] ON [Orders].[PaymentAttempts] ([TenantId], [OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830172740_AddPaymentAttempts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentAttempts_TenantId_UserId_IdempotencyKey] ON [Orders].[PaymentAttempts] ([TenantId], [UserId], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260830172740_AddPaymentAttempts'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260830172740_AddPaymentAttempts', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831191338_AddShipments'
)
BEGIN
    CREATE TABLE [Orders].[Shipments] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [ShippingMethod] nvarchar(100) NOT NULL,
        [Carrier] nvarchar(100) NOT NULL,
        [TrackingNumber] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [ShippedAt] datetime2 NULL,
        [DeliveredAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831191338_AddShipments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Shipments_TenantId_OrderId] ON [Orders].[Shipments] ([TenantId], [OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831191338_AddShipments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shipments_TenantId_TrackingNumber] ON [Orders].[Shipments] ([TenantId], [TrackingNumber]) WHERE [TrackingNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831191338_AddShipments'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260831191338_AddShipments', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831203455_AddShippingMethods'
)
BEGIN
    CREATE TABLE [Orders].[ShippingMethods] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Carrier] nvarchar(100) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_ShippingMethods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831203455_AddShippingMethods'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingMethods_TenantId_Code] ON [Orders].[ShippingMethods] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831203455_AddShippingMethods'
)
BEGIN
    CREATE INDEX [IX_ShippingMethods_TenantId_IsActive_SortOrder] ON [Orders].[ShippingMethods] ([TenantId], [IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260831203455_AddShippingMethods'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260831203455_AddShippingMethods', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    ALTER TABLE [Orders].[Orders] ADD [CouponCode] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    ALTER TABLE [Orders].[Orders] ADD [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    ALTER TABLE [Orders].[Orders] ADD [TaxRatePercent] decimal(9,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    ALTER TABLE [Orders].[Orders] ADD [TaxableAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE TABLE [Orders].[CouponRedemptions] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [CouponId] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CouponCode] nvarchar(64) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_CouponRedemptions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE TABLE [Orders].[Coupons] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [DiscountType] int NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [MinimumOrderAmount] decimal(18,2) NULL,
        [MaximumDiscountAmount] decimal(18,2) NULL,
        [StartsAt] datetime2 NULL,
        [ExpiresAt] datetime2 NULL,
        [UsageLimit] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Coupons] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE TABLE [Orders].[TaxRates] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [RatePercent] decimal(9,4) NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_TaxRates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE INDEX [IX_CouponRedemptions_TenantId_CouponId] ON [Orders].[CouponRedemptions] ([TenantId], [CouponId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CouponRedemptions_TenantId_CouponId_UserId] ON [Orders].[CouponRedemptions] ([TenantId], [CouponId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CouponRedemptions_TenantId_OrderId] ON [Orders].[CouponRedemptions] ([TenantId], [OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Coupons_TenantId_Code] ON [Orders].[Coupons] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE INDEX [IX_Coupons_TenantId_IsActive_ExpiresAt] ON [Orders].[Coupons] ([TenantId], [IsActive], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaxRates_TenantId_Code] ON [Orders].[TaxRates] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_TaxRates_TenantId_IsDefault] ON [Orders].[TaxRates] ([TenantId], [IsDefault]) WHERE [IsDefault] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260906153942_AddOrderPricingTaxAndCoupon'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260906153942_AddOrderPricingTaxAndCoupon', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260916172140_AddFulfillment'
)
BEGIN
    CREATE TABLE [Orders].[Fulfillments] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [Status] int NOT NULL,
        [WarehouseId] uniqueidentifier NULL,
        [PickingLocationId] uniqueidentifier NULL,
        [PickingStartedAt] datetime2 NULL,
        [PickedAt] datetime2 NULL,
        [PackingStartedAt] datetime2 NULL,
        [PackedAt] datetime2 NULL,
        [ReadyToShipAt] datetime2 NULL,
        [ShippedAt] datetime2 NULL,
        [DeliveredAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Fulfillments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Fulfillments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders].[Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260916172140_AddFulfillment'
)
BEGIN
    CREATE INDEX [IX_Fulfillments_OrderId] ON [Orders].[Fulfillments] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260916172140_AddFulfillment'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Fulfillments_TenantId_OrderId] ON [Orders].[Fulfillments] ([TenantId], [OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260916172140_AddFulfillment'
)
BEGIN
    CREATE INDEX [IX_Fulfillments_TenantId_Status_CreatedAt] ON [Orders].[Fulfillments] ([TenantId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260916172140_AddFulfillment'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260916172140_AddFulfillment', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    ALTER TABLE [Orders].[Orders] ADD [ShippingMethodId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    CREATE TABLE [Orders].[Packages] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [FulfillmentId] uniqueidentifier NOT NULL,
        [PackageNumber] int NOT NULL,
        [Status] int NOT NULL,
        [TrackingNumber] nvarchar(200) NULL,
        [WeightKg] decimal(10,3) NULL,
        [LengthCm] decimal(10,2) NULL,
        [WidthCm] decimal(10,2) NULL,
        [HeightCm] decimal(10,2) NULL,
        [PackingStartedAt] datetime2 NULL,
        [PackedAt] datetime2 NULL,
        [LabelPrintedAt] datetime2 NULL,
        [ShippedAt] datetime2 NULL,
        [DeliveredAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Packages_Fulfillments_FulfillmentId] FOREIGN KEY ([FulfillmentId]) REFERENCES [Orders].[Fulfillments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Packages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders].[Orders] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    CREATE INDEX [IX_Packages_FulfillmentId] ON [Orders].[Packages] ([FulfillmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    CREATE INDEX [IX_Packages_OrderId] ON [Orders].[Packages] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    CREATE INDEX [IX_Packages_TenantId_FulfillmentId] ON [Orders].[Packages] ([TenantId], [FulfillmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Packages_TenantId_OrderId_PackageNumber] ON [Orders].[Packages] ([TenantId], [OrderId], [PackageNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    CREATE INDEX [IX_Packages_TenantId_TrackingNumber] ON [Orders].[Packages] ([TenantId], [TrackingNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20260924040858_AddOrderShippingMethod'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20260924040858_AddOrderShippingMethod', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261007074749_AddOrderItemVariantAttributes'
)
BEGIN
    ALTER TABLE [Orders].[OrderItems] ADD [VariantAttributesJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261007074749_AddOrderItemVariantAttributes'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20261007074749_AddOrderItemVariantAttributes', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261022181252_AddPointOfSale'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261022181252_AddPointOfSale'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261022181252_AddPointOfSale'
)
BEGIN
    CREATE INDEX [IX_PosSaleItems_PosSaleId] ON [Orders].[PosSaleItems] ([PosSaleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261022181252_AddPointOfSale'
)
BEGIN
    CREATE INDEX [IX_PosSaleItems_ProductVariantId] ON [Orders].[PosSaleItems] ([ProductVariantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261022181252_AddPointOfSale'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PosSales_TenantId_SaleNumber] ON [Orders].[PosSales] ([TenantId], [SaleNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261022181252_AddPointOfSale'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PosSales_TenantId_SellerUserId_IdempotencyKey] ON [Orders].[PosSales] ([TenantId], [SellerUserId], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261022181252_AddPointOfSale'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20261022181252_AddPointOfSale', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    ALTER TABLE [Orders].[PosSales] ADD [BridgeDeviceId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    ALTER TABLE [Orders].[PosSales] ADD [PaymentGatewayResponse] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    ALTER TABLE [Orders].[PosSales] ADD [PaymentStartedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    CREATE TABLE [Orders].[PosBridgeDevices] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] nvarchar(64) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [LastSeenAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PosBridgeDevices] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    CREATE INDEX [IX_PosSales_BridgeDeviceId] ON [Orders].[PosSales] ([BridgeDeviceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PosSales_TenantId_SellerUserId] ON [Orders].[PosSales] ([TenantId], [SellerUserId]) WHERE [PaymentMethod] = 2 AND [Status] IN (1, 4, 5)');
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    CREATE INDEX [IX_PosSales_TenantId_Status_PaymentMethod_CreatedAtUtc] ON [Orders].[PosSales] ([TenantId], [Status], [PaymentMethod], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PosBridgeDevices_TenantId] ON [Orders].[PosBridgeDevices] ([TenantId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PosBridgeDevices_TokenHash] ON [Orders].[PosBridgeDevices] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    ALTER TABLE [Orders].[PosSales] ADD CONSTRAINT [FK_PosSales_PosBridgeDevices_BridgeDeviceId] FOREIGN KEY ([BridgeDeviceId]) REFERENCES [Orders].[PosBridgeDevices] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory_Orders]
    WHERE [MigrationId] = N'20261103151653_AddPosBridgeIntegration'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory_Orders] ([MigrationId], [ProductVersion])
    VALUES (N'20261103151653_AddPosBridgeIntegration', N'10.0.10');
END;

COMMIT;
GO

