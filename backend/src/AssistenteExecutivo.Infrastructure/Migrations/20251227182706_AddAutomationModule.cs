using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "Highlighted",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateTable(
                name: "DraftDocuments",
                columns: table => new
                {
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    LetterheadId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftDocuments", x => x.DraftId);
                });

            migrationBuilder.CreateTable(
                name: "Letterheads",
                columns: table => new
                {
                    LetterheadId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DesignData = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Letterheads", x => x.LetterheadId);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    ReminderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SuggestedMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.ReminderId);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    PlaceholdersSchema = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.TemplateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DraftDocuments_CompanyId",
                table: "DraftDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftDocuments_ContactId",
                table: "DraftDocuments",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftDocuments_DocumentType",
                table: "DraftDocuments",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_DraftDocuments_OwnerUserId",
                table: "DraftDocuments",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftDocuments_OwnerUserId_Status",
                table: "DraftDocuments",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DraftDocuments_Status",
                table: "DraftDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Letterheads_IsActive",
                table: "Letterheads",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Letterheads_OwnerUserId",
                table: "Letterheads",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Letterheads_OwnerUserId_IsActive",
                table: "Letterheads",
                columns: new[] { "OwnerUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ContactId",
                table: "Reminders",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_OwnerUserId",
                table: "Reminders",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_OwnerUserId_Status",
                table: "Reminders",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ScheduledFor",
                table: "Reminders",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_Status",
                table: "Reminders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_Status_ScheduledFor",
                table: "Reminders",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Active",
                table: "Templates",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_OwnerUserId",
                table: "Templates",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_OwnerUserId_Active",
                table: "Templates",
                columns: new[] { "OwnerUserId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_OwnerUserId_Type_Active",
                table: "Templates",
                columns: new[] { "OwnerUserId", "Type", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Type",
                table: "Templates",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DraftDocuments");

            migrationBuilder.DropTable(
                name: "Letterheads");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Plans",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Highlighted",
                table: "Plans",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
