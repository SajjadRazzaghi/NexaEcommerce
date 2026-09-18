using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    City = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Rack = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Shelf = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Bin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHandQuantity = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    IncomingQuantity = table.Column<int>(type: "int", nullable: false),
                    DamagedQuantity = table.Column<int>(type: "int", nullable: false),
                    ReorderPoint = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_TenantId_WarehouseId_Code",
                schema: "Inventory",
                table: "WarehouseLocations",
                columns: new[] { "TenantId", "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_WarehouseId",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_Code",
                schema: "Inventory",
                table: "Warehouses",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_IsDefault",
                schema: "Inventory",
                table: "Warehouses",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_LocationId",
                schema: "Inventory",
                table: "WarehouseStocks",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_TenantId_ProductVariantId",
                schema: "Inventory",
                table: "WarehouseStocks",
                columns: new[] { "TenantId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_TenantId_WarehouseId_LocationId_ProductVariantId",
                schema: "Inventory",
                table: "WarehouseStocks",
                columns: new[] { "TenantId", "WarehouseId", "LocationId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_WarehouseId",
                schema: "Inventory",
                table: "WarehouseStocks",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseStocks",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "WarehouseLocations",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "Inventory");
        }
    }
}
