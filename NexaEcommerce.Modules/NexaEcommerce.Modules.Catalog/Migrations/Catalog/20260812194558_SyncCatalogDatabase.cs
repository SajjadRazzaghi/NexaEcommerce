using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class SyncCatalogDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                schema: "Catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Color",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Size",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Barcode",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CompareCurrency",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountEndDate",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountStartDate",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "IsInStock",
                schema: "Catalog",
                table: "Products",
                newName: "IsPublished");

            migrationBuilder.RenameColumn(
                name: "IsVerifiedPurchase",
                schema: "Catalog",
                table: "ProductReviews",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "IsMain",
                schema: "Catalog",
                table: "ProductImages",
                newName: "IsPrimary");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                schema: "Catalog",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceOverride",
                schema: "Catalog",
                table: "ProductVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComparePrice",
                schema: "Catalog",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "Catalog",
                table: "ProductVariants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "Catalog",
                table: "ProductVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "Catalog",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Catalog",
                table: "ProductVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShortDescription",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercentage",
                schema: "Catalog",
                table: "Products",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "Catalog",
                table: "ProductReviews",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "Catalog",
                table: "ProductReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                schema: "Catalog",
                table: "ProductReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Catalog",
                table: "ProductReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "Catalog",
                table: "ProductImages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "AltText",
                schema: "Catalog",
                table: "ProductImages",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "Catalog",
                table: "ProductImages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "Catalog",
                table: "ProductImages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "Catalog",
                table: "ProductImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Catalog",
                table: "ProductImages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributes_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttributeValues",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductAttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ColorHex = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeValues_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalSchema: "Catalog",
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariantAttributeValues",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeValueId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantAttributeValues_AttributeValues_AttributeValueId",
                        column: x => x.AttributeValueId,
                        principalSchema: "Catalog",
                        principalTable: "AttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VariantAttributeValues_AttributeValues_AttributeValueId1",
                        column: x => x.AttributeValueId1,
                        principalSchema: "Catalog",
                        principalTable: "AttributeValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VariantAttributeValues_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalSchema: "Catalog",
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                schema: "Catalog",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "Catalog",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValues_ProductAttributeId_Value",
                schema: "Catalog",
                table: "AttributeValues",
                columns: new[] { "ProductAttributeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ProductId_Code",
                schema: "Catalog",
                table: "ProductAttributes",
                columns: new[] { "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantAttributeValues_AttributeValueId",
                schema: "Catalog",
                table: "VariantAttributeValues",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_VariantAttributeValues_AttributeValueId1",
                schema: "Catalog",
                table: "VariantAttributeValues",
                column: "AttributeValueId1");

            migrationBuilder.CreateIndex(
                name: "IX_VariantAttributeValues_ProductVariantId_AttributeValueId",
                schema: "Catalog",
                table: "VariantAttributeValues",
                columns: new[] { "ProductVariantId", "AttributeValueId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VariantAttributeValues",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AttributeValues",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "ProductAttributes",
                schema: "Catalog");

            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                schema: "Catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "ComparePrice",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Catalog",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "Catalog",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                schema: "Catalog",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Catalog",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "Catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "Catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Catalog",
                table: "ProductImages");

            migrationBuilder.RenameColumn(
                name: "IsPublished",
                schema: "Catalog",
                table: "Products",
                newName: "IsInStock");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                schema: "Catalog",
                table: "ProductReviews",
                newName: "IsVerifiedPurchase");

            migrationBuilder.RenameColumn(
                name: "IsPrimary",
                schema: "Catalog",
                table: "ProductImages",
                newName: "IsMain");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                schema: "Catalog",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceOverride",
                schema: "Catalog",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                schema: "Catalog",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                schema: "Catalog",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ShortDescription",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercentage",
                schema: "Catalog",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompareCurrency",
                schema: "Catalog",
                table: "Products",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountEndDate",
                schema: "Catalog",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountStartDate",
                schema: "Catalog",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                schema: "Catalog",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                schema: "Catalog",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "Catalog",
                table: "ProductReviews",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "Catalog",
                table: "ProductImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "AltText",
                schema: "Catalog",
                table: "ProductImages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                schema: "Catalog",
                table: "Products",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "Catalog",
                table: "Categories",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");
        }
    }
}
