using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchContextInstructionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "ai_batch_context_instruction", "IMPORTANT: Some items in the batch are marked with \"isContextOnly\": true. These are provided ONLY for context to help you understand the conversation flow. Do NOT translate or include context-only items in your output. Only translate and return items where \"isContextOnly\" is false or not present." },
                    { "ai_batch_context_instruction_editable", "false" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "ai_batch_context_instruction",
                    "ai_batch_context_instruction_editable"
                });
        }
    }
}
