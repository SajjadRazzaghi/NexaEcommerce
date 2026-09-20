using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class SyncCatalogModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantAttributeValues_ProductVariants_ProductVariantId",
                schema: "Catalog",
                table: "VariantAttributeValues");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantAttributeValues_ProductVariants_ProductVariantId",
                schema: "Catalog",
                table: "VariantAttributeValues",
                column: "ProductVariantId",
                principalSchema: "Catalog",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantAttributeValues_ProductVariants_ProductVariantId",
                schema: "Catalog",
                table: "VariantAttributeValues");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantAttributeValues_ProductVariants_ProductVariantId",
                schema: "Catalog",
                table: "VariantAttributeValues",
                column: "ProductVariantId",
                principalSchema: "Catalog",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
