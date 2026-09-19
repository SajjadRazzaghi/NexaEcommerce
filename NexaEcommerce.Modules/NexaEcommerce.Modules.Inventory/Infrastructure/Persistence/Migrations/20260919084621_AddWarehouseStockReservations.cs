using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseStockReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseStockReservations",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FulfillmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderInventoryReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStockReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStockReservations_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStockReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockReservations_LocationId",
                schema: "Inventory",
                table: "WarehouseStockReservations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockReservations_TenantId_OrderId_Status",
                schema: "Inventory",
                table: "WarehouseStockReservations",
                columns: new[] { "TenantId", "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockReservations_TenantId_OrderInventoryReservationId_WarehouseId_LocationId",
                schema: "Inventory",
                table: "WarehouseStockReservations",
                columns: new[] { "TenantId", "OrderInventoryReservationId", "WarehouseId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockReservations_TenantId_ProductVariantId_WarehouseId_LocationId",
                schema: "Inventory",
                table: "WarehouseStockReservations",
                columns: new[] { "TenantId", "ProductVariantId", "WarehouseId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockReservations_TenantId_ReservationKey",
                schema: "Inventory",
                table: "WarehouseStockReservations",
                columns: new[] { "TenantId", "ReservationKey" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockReservations_WarehouseId",
                schema: "Inventory",
                table: "WarehouseStockReservations",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseStockReservations",
                schema: "Inventory");
        }
    }
}
