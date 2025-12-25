using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileContentToMediaAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileContent",
                table: "MediaAssets",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileContent",
                table: "MediaAssets");
        }
    }
}
