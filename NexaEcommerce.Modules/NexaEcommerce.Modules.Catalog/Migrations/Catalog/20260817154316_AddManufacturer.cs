using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                schema: "Catalog",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Manufacturers",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SeoTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SeoKeywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manufacturers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ManufacturerId",
                schema: "Catalog",
                table: "Products",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_DisplayOrder_Name",
                schema: "Catalog",
                table: "Manufacturers",
                columns: new[] { "DisplayOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_IsActive_IsPublished_IsFeatured",
                schema: "Catalog",
                table: "Manufacturers",
                columns: new[] { "IsActive", "IsPublished", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_Name",
                schema: "Catalog",
                table: "Manufacturers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_Slug",
                schema: "Catalog",
                table: "Manufacturers",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Manufacturers_ManufacturerId",
                schema: "Catalog",
                table: "Products",
                column: "ManufacturerId",
                principalSchema: "Catalog",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Manufacturers_ManufacturerId",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Manufacturers",
                schema: "Catalog");

            migrationBuilder.DropIndex(
                name: "IX_Products_ManufacturerId",
                schema: "Catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                schema: "Catalog",
                table: "Products");
        }
    }
}
