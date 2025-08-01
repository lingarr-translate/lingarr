﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
{
    /// <inheritdoc />
    public partial class BatchTranslationAndTagging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "use_batch_translation", "false" },
                    { "max_batch_size", "0" },
                    { "use_subtitle_tagging", "false" },
                    { "subtitle_tag", "lingarr" }
                });
            
            migrationBuilder.AddColumn<int>(
                name: "media_id",
                table: "translation_requests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "use_batch_translation",
                    "max_batch_size",
                    "use_subtitle_tagging",
                    "subtitle_tag"
                });
            
            migrationBuilder.DropColumn(
                name: "media_id",
                table: "translation_requests");
        }
    }
}
