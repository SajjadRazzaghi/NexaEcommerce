using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosBridgeIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BridgeDeviceId",
                schema: "Orders",
                table: "PosSales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayResponse",
                schema: "Orders",
                table: "PosSales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentStartedAtUtc",
                schema: "Orders",
                table: "PosSales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PosBridgeDevices",
                schema: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosBridgeDevices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_BridgeDeviceId",
                schema: "Orders",
                table: "PosSales",
                column: "BridgeDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_TenantId_SellerUserId",
                schema: "Orders",
                table: "PosSales",
                columns: new[] { "TenantId", "SellerUserId" },
                unique: true,
                filter: "[PaymentMethod] = 2 AND [Status] IN (1, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_TenantId_Status_PaymentMethod_CreatedAtUtc",
                schema: "Orders",
                table: "PosSales",
                columns: new[] { "TenantId", "Status", "PaymentMethod", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PosBridgeDevices_TenantId",
                schema: "Orders",
                table: "PosBridgeDevices",
                column: "TenantId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PosBridgeDevices_TokenHash",
                schema: "Orders",
                table: "PosBridgeDevices",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PosSales_PosBridgeDevices_BridgeDeviceId",
                schema: "Orders",
                table: "PosSales",
                column: "BridgeDeviceId",
                principalSchema: "Orders",
                principalTable: "PosBridgeDevices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PosSales_PosBridgeDevices_BridgeDeviceId",
                schema: "Orders",
                table: "PosSales");

            migrationBuilder.DropTable(
                name: "PosBridgeDevices",
                schema: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_PosSales_BridgeDeviceId",
                schema: "Orders",
                table: "PosSales");

            migrationBuilder.DropIndex(
                name: "IX_PosSales_TenantId_SellerUserId",
                schema: "Orders",
                table: "PosSales");

            migrationBuilder.DropIndex(
                name: "IX_PosSales_TenantId_Status_PaymentMethod_CreatedAtUtc",
                schema: "Orders",
                table: "PosSales");

            migrationBuilder.DropColumn(
                name: "BridgeDeviceId",
                schema: "Orders",
                table: "PosSales");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayResponse",
                schema: "Orders",
                table: "PosSales");

            migrationBuilder.DropColumn(
                name: "PaymentStartedAtUtc",
                schema: "Orders",
                table: "PosSales");
        }
    }
}
