using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POC.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeatId = table.Column<long>(type: "INTEGER", nullable: false),
                    InitialAmount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vouchers_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoucherHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoucherId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoucherHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoucherHistories_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoucherHistories_OrderId",
                table: "VoucherHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherHistories_VoucherId",
                table: "VoucherHistories",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_SeatId",
                table: "Vouchers",
                column: "SeatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoucherHistories");

            migrationBuilder.DropTable(
                name: "Vouchers");
        }
    }
}
