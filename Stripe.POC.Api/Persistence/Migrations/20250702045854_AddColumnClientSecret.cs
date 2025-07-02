using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POC.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnClientSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "CheckoutSessions",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "CheckoutSessions");
        }
    }
}
