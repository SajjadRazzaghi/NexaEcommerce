using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class ExpandBrandSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "Catalog",
                table: "Brands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                schema: "Catalog",
                table: "Brands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "Catalog",
                table: "Brands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "Catalog",
                table: "Brands",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoKeywords",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_DisplayOrder_Name",
                schema: "Catalog",
                table: "Brands",
                columns: new[] { "DisplayOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_IsActive_IsPublished_IsFeatured",
                schema: "Catalog",
                table: "Brands",
                columns: new[] { "IsActive", "IsPublished", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                schema: "Catalog",
                table: "Brands",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_DisplayOrder_Name",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_IsActive_IsPublished_IsFeatured",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Name",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "SeoKeywords",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Website",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                schema: "Catalog",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
