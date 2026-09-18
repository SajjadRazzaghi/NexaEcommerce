using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AvailableDelta = table.Column<int>(type: "int", nullable: false),
                    ReservedDelta = table.Column<int>(type: "int", nullable: false),
                    AvailableBalance = table.Column<int>(type: "int", nullable: false),
                    ReservedBalance = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Inventory",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_StockItemId",
                schema: "Inventory",
                table: "InventoryMovements",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_ProductVariantId_OccurredAt",
                schema: "Inventory",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "ProductVariantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_ReferenceType_ReferenceId",
                schema: "Inventory",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryMovements",
                schema: "Inventory");
        }
    }
}
