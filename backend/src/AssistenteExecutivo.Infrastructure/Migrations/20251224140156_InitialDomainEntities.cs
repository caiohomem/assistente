using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditBalance",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domains = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyId);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.ContactId);
                });

            migrationBuilder.CreateTable(
                name: "CreditWallets",
                columns: table => new
                {
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditWallets", x => x.OwnerUserId);
                    table.ForeignKey(
                        name: "FK_CreditWallets_UserProfiles_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    MediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.MediaId);
                });

            migrationBuilder.CreateTable(
                name: "ContactEmails",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactEmails", x => new { x.ContactId, x.Email });
                    table.ForeignKey(
                        name: "FK_ContactEmails_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactPhones",
                columns: table => new
                {
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormattedNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactPhones", x => new { x.ContactId, x.Number });
                    table.ForeignKey(
                        name: "FK_ContactPhones_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactTags",
                columns: table => new
                {
                    Tag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactTags", x => new { x.ContactId, x.Tag });
                    table.ForeignKey(
                        name: "FK_ContactTags_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RawContent = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StructuredData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.NoteId);
                    table.ForeignKey(
                        name: "FK_Notes_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    RelationshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Strength = table.Column<float>(type: "real", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.RelationshipId);
                    table.ForeignKey(
                        name: "FK_Relationships_Contacts_SourceContactId",
                        column: x => x.SourceContactId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_Contacts_TargetContactId",
                        column: x => x.TargetContactId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_CreditWallets_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "CreditWallets",
                        principalColumn: "OwnerUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_UserProfiles_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaptureJobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CardScanResult_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CardScanResult_Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CardScanResult_Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardScanResult_Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CardScanResult_JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CardScanResult_ConfidenceScores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioTranscript_Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptureJobs", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_CaptureJobs_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaptureJobs_MediaAssets_MediaId",
                        column: x => x.MediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "MediaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaptureJobs_UserProfiles_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaptureJobExtractedTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptureJobExtractedTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaptureJobExtractedTasks_CaptureJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "CaptureJobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaptureJobExtractedTasks_JobId",
                table: "CaptureJobExtractedTasks",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptureJobs_ContactId",
                table: "CaptureJobs",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptureJobs_MediaId",
                table: "CaptureJobs",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptureJobs_OwnerUserId",
                table: "CaptureJobs",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptureJobs_RequestedAt",
                table: "CaptureJobs",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CaptureJobs_Status",
                table: "CaptureJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CaptureJobs_Type",
                table: "CaptureJobs",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_CreatedAt",
                table: "Contacts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_IsDeleted",
                table: "Contacts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_OwnerUserId",
                table: "Contacts",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_OwnerUserId_IsDeleted",
                table: "Contacts",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_IdempotencyKey",
                table: "CreditTransactions",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_OccurredAt",
                table: "CreditTransactions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_OwnerUserId",
                table: "CreditTransactions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Type",
                table: "CreditTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_CreatedAt",
                table: "MediaAssets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Kind",
                table: "MediaAssets",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_OwnerUserId",
                table: "MediaAssets",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_AuthorId",
                table: "Notes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ContactId",
                table: "Notes",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedAt",
                table: "Notes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_Type",
                table: "Notes",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_SourceContactId",
                table: "Relationships",
                column: "SourceContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_TargetContactId",
                table: "Relationships",
                column: "TargetContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_Type",
                table: "Relationships",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "UQ_Relationships_SourceContactId_TargetContactId",
                table: "Relationships",
                columns: new[] { "SourceContactId", "TargetContactId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaptureJobExtractedTasks");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "ContactEmails");

            migrationBuilder.DropTable(
                name: "ContactPhones");

            migrationBuilder.DropTable(
                name: "ContactTags");

            migrationBuilder.DropTable(
                name: "CreditTransactions");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "CaptureJobs");

            migrationBuilder.DropTable(
                name: "CreditWallets");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "CreditBalance",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "UserProfiles");
        }
    }
}
