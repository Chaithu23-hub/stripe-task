using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StripePayment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueCheckoutRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_GuestId_RequestId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_GuestId",
                table: "Orders",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RequestId",
                table: "Orders",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_GuestId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_RequestId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_GuestId_RequestId",
                table: "Orders",
                columns: new[] { "GuestId", "RequestId" },
                unique: true);
        }
    }
}
