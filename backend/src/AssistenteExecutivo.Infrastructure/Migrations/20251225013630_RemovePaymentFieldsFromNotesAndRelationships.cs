using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentFieldsFromNotesAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "PaidWithCredits",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "PaidWithCredits",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "Notes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Relationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Relationships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidWithCredits",
                table: "Relationships",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentTransactionId",
                table: "Relationships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Notes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Notes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidWithCredits",
                table: "Notes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentTransactionId",
                table: "Notes",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
