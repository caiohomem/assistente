using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Workflows",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WorkflowExecutions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_IdempotencyKey",
                table: "Workflows",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_IdempotencyKey",
                table: "WorkflowExecutions",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workflows_IdempotencyKey",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutions_IdempotencyKey",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WorkflowExecutions");
        }
    }
}
