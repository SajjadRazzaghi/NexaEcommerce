using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddCatalogAttributeUsageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                schema: "Catalog",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CombinationKey",
                schema: "Catalog",
                table: "ProductVariants",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "Catalog",
                table: "ProductAttributes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                schema: "Catalog",
                table: "ProductAttributes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "Catalog",
                table: "ProductAttributes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayType",
                schema: "Catalog",
                table: "CatalogAttributes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Catalog",
                table: "CatalogAttributes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DataType",
                schema: "Catalog",
                table: "CatalogAttributes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoryAttributes",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogAttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_CatalogAttributes_CatalogAttributeId",
                        column: x => x.CatalogAttributeId,
                        principalSchema: "Catalog",
                        principalTable: "CatalogAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "Catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantImages",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantImages_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalSchema: "Catalog",
                        principalTable: "ProductVariants",
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
                name: "IX_ProductAttributes_CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes",
                column: "CatalogAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId_CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes",
                columns: new[] { "ProductId", "CatalogAttributeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValues_CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues",
                column: "CatalogAttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValues_ProductAttributeId_CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues",
                columns: new[] { "ProductAttributeId", "CatalogAttributeValueId" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CatalogAttributeId",
                schema: "Catalog",
                table: "CategoryAttributes",
                column: "CatalogAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId_CatalogAttributeId",
                schema: "Catalog",
                table: "CategoryAttributes",
                columns: new[] { "CategoryId", "CatalogAttributeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId_DisplayOrder",
                schema: "Catalog",
                table: "CategoryAttributes",
                columns: new[] { "CategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantImages_ProductVariantId_DisplayOrder",
                schema: "Catalog",
                table: "ProductVariantImages",
                columns: new[] { "ProductVariantId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantImages_ProductVariantId_IsPrimary",
                schema: "Catalog",
                table: "ProductVariantImages",
                columns: new[] { "ProductVariantId", "IsPrimary" });

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeValues_CatalogAttributeValues_CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues",
                column: "CatalogAttributeValueId",
                principalSchema: "Catalog",
                principalTable: "CatalogAttributeValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_CatalogAttributes_CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes",
                column: "CatalogAttributeId",
                principalSchema: "Catalog",
                principalTable: "CatalogAttributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeValues_CatalogAttributeValues_CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_CatalogAttributes_CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes");

            migrationBuilder.DropTable(
                name: "CategoryAttributes",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "ProductVariantImages",
                schema: "Catalog");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_ProductId_CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_AttributeValues_CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_AttributeValues_ProductAttributeId_CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues");

            migrationBuilder.DropColumn(
                name: "Barcode",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CombinationKey",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CatalogAttributeId",
                schema: "Catalog",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "Catalog",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                schema: "Catalog",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "Catalog",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "DataType",
                schema: "Catalog",
                table: "CatalogAttributes");

            migrationBuilder.DropColumn(
                name: "CatalogAttributeValueId",
                schema: "Catalog",
                table: "AttributeValues");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayType",
                schema: "Catalog",
                table: "CatalogAttributes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Catalog",
                table: "CatalogAttributes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                schema: "Catalog",
                table: "ProductVariants",
                column: "ProductId");
        }
    }
}
