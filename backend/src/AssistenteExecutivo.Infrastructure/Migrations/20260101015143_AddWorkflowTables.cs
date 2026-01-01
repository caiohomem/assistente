using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteExecutivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowExecutions",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecVersionUsed = table.Column<int>(type: "integer", nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: true),
                    OutputJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    N8nExecutionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CurrentStepIndex = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutions", x => x.ExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TriggerType = table.Column<int>(type: "integer", nullable: false),
                    TriggerCronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TriggerEventName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TriggerConfigJson = table.Column<string>(type: "jsonb", nullable: true),
                    SpecJson = table.Column<string>(type: "jsonb", nullable: false),
                    SpecVersion = table.Column<int>(type: "integer", nullable: false),
                    N8nWorkflowId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.WorkflowId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_N8nExecutionId",
                table: "WorkflowExecutions",
                column: "N8nExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_OwnerUserId",
                table: "WorkflowExecutions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_OwnerUserId_Status",
                table: "WorkflowExecutions",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_StartedAt",
                table: "WorkflowExecutions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_Status",
                table: "WorkflowExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_WorkflowId",
                table: "WorkflowExecutions",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_N8nWorkflowId",
                table: "Workflows",
                column: "N8nWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_OwnerUserId",
                table: "Workflows",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_OwnerUserId_Name",
                table: "Workflows",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_OwnerUserId_Status",
                table: "Workflows",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Status",
                table: "Workflows",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowExecutions");

            migrationBuilder.DropTable(
                name: "Workflows");
        }
    }
}
