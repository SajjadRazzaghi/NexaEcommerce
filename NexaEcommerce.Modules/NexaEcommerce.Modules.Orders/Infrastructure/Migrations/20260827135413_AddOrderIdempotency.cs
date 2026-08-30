using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "Orders",
                table: "Orders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_UserId_IdempotencyKey",
                schema: "Orders",
                table: "Orders",
                columns: new[] { "TenantId", "UserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_UserId_IdempotencyKey",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "Orders",
                table: "Orders");
        }
    }
}
