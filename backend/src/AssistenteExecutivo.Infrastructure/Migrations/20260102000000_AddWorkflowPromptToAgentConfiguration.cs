using Microsoft.EntityFrameworkCore.Migrations;

namespace AssistenteExecutivo.Infrastructure.Migrations;

public partial class AddWorkflowPromptToAgentConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WorkflowPrompt",
            table: "AgentConfigurations",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WorkflowPrompt",
            table: "AgentConfigurations");
    }
}
