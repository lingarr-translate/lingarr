using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media");

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "seasons",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "media_hash",
                table: "movies",
                type: "TEXT",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "media_hash",
                table: "episodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    path = table.Column<string>(type: "TEXT", nullable: false),
                    show_id = table.Column<int>(type: "INTEGER", nullable: true),
                    movie_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_images_movies_movie_id",
                        column: x => x.movie_id,
                        principalTable: "movies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_images_shows_show_id",
                        column: x => x.show_id,
                        principalTable: "shows",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_images_movie_id",
                table: "images",
                column: "movie_id");

            migrationBuilder.CreateIndex(
                name: "ix_images_show_id",
                table: "images",
                column: "show_id");
            
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "automation_enabled", "false" },
                    { "service_type", "libretranslate" },
                    { "libretranslate_url", Environment.GetEnvironmentVariable("LIBRETRANSLATE_API") ?? "http://libretranslate:5000" },
                    { "max_translations_per_run", "10" },
                    { "deepl_api_key", "" },
                    { "translation_schedule", "0 4 * * *" },
                    { "translation_cycle", "false" }
                });
            
            migrationBuilder.DropForeignKey(
                name: "fk_images_movies_movie_id",
                table: "images");

            migrationBuilder.DropForeignKey(
                name: "fk_images_shows_show_id",
                table: "images");

            migrationBuilder.AddForeignKey(
                name: "fk_images_movies_movie_id",
                table: "images",
                column: "movie_id",
                principalTable: "movies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_images_shows_show_id",
                table: "images",
                column: "show_id",
                principalTable: "shows",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "images");

            migrationBuilder.DropColumn(
                name: "media_hash",
                table: "movies");

            migrationBuilder.DropColumn(
                name: "media_hash",
                table: "episodes");

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "seasons",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "media",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    movie_id = table.Column<int>(type: "INTEGER", nullable: true),
                    show_id = table.Column<int>(type: "INTEGER", nullable: true),
                    path = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_movies_movie_id",
                        column: x => x.movie_id,
                        principalTable: "movies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_media_shows_show_id",
                        column: x => x.show_id,
                        principalTable: "shows",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_movie_id",
                table: "media",
                column: "movie_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_show_id",
                table: "media",
                column: "show_id");
            
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "automation_enabled",
                    "service_type",
                    "libretranslate_url",
                    "max_translations_per_run",
                    "deepl_api_key",
                    "translation_schedule",
                    "translation_cycle"
                });
            
            migrationBuilder.DropForeignKey(
                name: "fk_images_movies_movie_id",
                table: "images");

            migrationBuilder.DropForeignKey(
                name: "fk_images_shows_show_id",
                table: "images");

            migrationBuilder.AddForeignKey(
                name: "fk_images_movies_movie_id",
                table: "images",
                column: "movie_id",
                principalTable: "movies",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_images_shows_show_id",
                table: "images",
                column: "show_id",
                principalTable: "shows",
                principalColumn: "id");
        }
    }
}
