using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddCategorySeoMetadataAndPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SeoTitle",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SeoKeywords",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                schema: "Catalog",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "Catalog",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DisplayOrder_Name",
                schema: "Catalog",
                table: "Categories",
                columns: new[] { "DisplayOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive_IsPublished_IsFeatured",
                schema: "Catalog",
                table: "Categories",
                columns: new[] { "IsActive", "IsPublished", "IsFeatured" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_DisplayOrder_Name",
                schema: "Catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive_IsPublished_IsFeatured",
                schema: "Catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                schema: "Catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "Catalog",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "SeoTitle",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SeoKeywords",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                schema: "Catalog",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
