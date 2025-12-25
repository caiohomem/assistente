using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardScanResultRawText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardScanResult_RawText",
                table: "CaptureJobs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardScanResult_RawText",
                table: "CaptureJobs");
        }
    }
}
