using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelationshipTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelationshipTypeId",
                table: "Relationships",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RelationshipTypes",
                columns: table => new
                {
                    RelationshipTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipTypes", x => x.RelationshipTypeId);
                    table.ForeignKey(
                        name: "FK_RelationshipTypes_UserProfiles_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_RelationshipTypeId",
                table: "Relationships",
                column: "RelationshipTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_RelationshipTypes_Owner_Name",
                table: "RelationshipTypes",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Relationships_RelationshipTypes_RelationshipTypeId",
                table: "Relationships",
                column: "RelationshipTypeId",
                principalTable: "RelationshipTypes",
                principalColumn: "RelationshipTypeId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Relationships_RelationshipTypes_RelationshipTypeId",
                table: "Relationships");

            migrationBuilder.DropTable(
                name: "RelationshipTypes");

            migrationBuilder.DropIndex(
                name: "IX_Relationships_RelationshipTypeId",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "RelationshipTypeId",
                table: "Relationships");
        }
    }
}
