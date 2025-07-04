using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POC.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedUniqeConstraintOnSessionInPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Payments_SessionId",
                table: "Payments",
                column: "SessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_SessionId",
                table: "Payments");
        }
    }
}
