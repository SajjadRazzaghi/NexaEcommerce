using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fulfillments",
                schema: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PickingLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PickingStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackingStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadyToShipAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fulfillments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fulfillments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fulfillments_OrderId",
                schema: "Orders",
                table: "Fulfillments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Fulfillments_TenantId_OrderId",
                schema: "Orders",
                table: "Fulfillments",
                columns: new[] { "TenantId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fulfillments_TenantId_Status_CreatedAt",
                schema: "Orders",
                table: "Fulfillments",
                columns: new[] { "TenantId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fulfillments",
                schema: "Orders");
        }
    }
}
