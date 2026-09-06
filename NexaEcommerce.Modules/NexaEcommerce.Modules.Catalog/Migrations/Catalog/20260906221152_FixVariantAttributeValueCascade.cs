using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Catalog.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class FixVariantAttributeValueCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantAttributeValues_AttributeValues_AttributeValueId1",
                schema: "Catalog",
                table: "VariantAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_VariantAttributeValues_AttributeValueId1",
                schema: "Catalog",
                table: "VariantAttributeValues");

            migrationBuilder.DropColumn(
                name: "AttributeValueId1",
                schema: "Catalog",
                table: "VariantAttributeValues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AttributeValueId1",
                schema: "Catalog",
                table: "VariantAttributeValues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantAttributeValues_AttributeValueId1",
                schema: "Catalog",
                table: "VariantAttributeValues",
                column: "AttributeValueId1");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantAttributeValues_AttributeValues_AttributeValueId1",
                schema: "Catalog",
                table: "VariantAttributeValues",
                column: "AttributeValueId1",
                principalSchema: "Catalog",
                principalTable: "AttributeValues",
                principalColumn: "Id");
        }
    }
}
