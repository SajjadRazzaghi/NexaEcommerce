using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddProductFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.CreateTable(
                name: "ProductFavorites",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    GuestKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFavorites_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                schema: "Catalog",
                table: "ProductVariants",
                columns: new[] { "ProductId", "CombinationKey" },
                unique: true,
                filter: "[CombinationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFavorites_ProductId",
                schema: "Catalog",
                table: "ProductFavorites",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFavorites_TenantId_CreatedAt",
                schema: "Catalog",
                table: "ProductFavorites",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFavorites_TenantId_ProductId_GuestKey",
                schema: "Catalog",
                table: "ProductFavorites",
                columns: new[] { "TenantId", "ProductId", "GuestKey" },
                unique: true,
                filter: "[GuestKey] IS NOT NULL AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFavorites_TenantId_ProductId_IsActive",
                schema: "Catalog",
                table: "ProductFavorites",
                columns: new[] { "TenantId", "ProductId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFavorites_TenantId_ProductId_UserId",
                schema: "Catalog",
                table: "ProductFavorites",
                columns: new[] { "TenantId", "ProductId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFavorites_TenantId_RemovedAt",
                schema: "Catalog",
                table: "ProductFavorites",
                columns: new[] { "TenantId", "RemovedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductFavorites",
                schema: "Catalog");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                schema: "Catalog",
                table: "ProductVariants",
                columns: new[] { "ProductId", "CombinationKey" },
                unique: true,
                filter: "[CombinationKey] IS NOT NULL AND [IsDeleted] = 0");
        }
    }
}
