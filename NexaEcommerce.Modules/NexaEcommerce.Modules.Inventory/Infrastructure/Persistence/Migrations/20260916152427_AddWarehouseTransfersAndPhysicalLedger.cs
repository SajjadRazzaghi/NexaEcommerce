using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseTransfersAndPhysicalLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseStockMovements",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStockMovements_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStockMovements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransfers",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_WarehouseLocations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_WarehouseLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockMovements_LocationId",
                schema: "Inventory",
                table: "WarehouseStockMovements",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockMovements_TenantId_ReferenceType_ReferenceId",
                schema: "Inventory",
                table: "WarehouseStockMovements",
                columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockMovements_TenantId_WarehouseId_LocationId_ProductVariantId_OccurredAt",
                schema: "Inventory",
                table: "WarehouseStockMovements",
                columns: new[] { "TenantId", "WarehouseId", "LocationId", "ProductVariantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockMovements_WarehouseId",
                schema: "Inventory",
                table: "WarehouseStockMovements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_DestinationLocationId",
                schema: "Inventory",
                table: "WarehouseTransfers",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_DestinationWarehouseId",
                schema: "Inventory",
                table: "WarehouseTransfers",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_SourceLocationId",
                schema: "Inventory",
                table: "WarehouseTransfers",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_SourceWarehouseId",
                schema: "Inventory",
                table: "WarehouseTransfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_TenantId_ProductVariantId_RequestedAt",
                schema: "Inventory",
                table: "WarehouseTransfers",
                columns: new[] { "TenantId", "ProductVariantId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_TenantId_RequestedAt",
                schema: "Inventory",
                table: "WarehouseTransfers",
                columns: new[] { "TenantId", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseStockMovements",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "WarehouseTransfers",
                schema: "Inventory");
        }
    }
}
