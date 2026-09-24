using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaEcommerce.Modules.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShippingMethodId",
                schema: "Orders",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Packages",
                schema: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FulfillmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: true),
                    LengthCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WidthCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    PackingStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LabelPrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_Fulfillments_FulfillmentId",
                        column: x => x.FulfillmentId,
                        principalSchema: "Orders",
                        principalTable: "Fulfillments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Packages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orders",
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_FulfillmentId",
                schema: "Orders",
                table: "Packages",
                column: "FulfillmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_OrderId",
                schema: "Orders",
                table: "Packages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_TenantId_FulfillmentId",
                schema: "Orders",
                table: "Packages",
                columns: new[] { "TenantId", "FulfillmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_TenantId_OrderId_PackageNumber",
                schema: "Orders",
                table: "Packages",
                columns: new[] { "TenantId", "OrderId", "PackageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_TenantId_TrackingNumber",
                schema: "Orders",
                table: "Packages",
                columns: new[] { "TenantId", "TrackingNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Packages",
                schema: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodId",
                schema: "Orders",
                table: "Orders");
        }
    }
}
