using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PEngineV.Migrations;

/// <inheritdoc />
public partial class AddWysiwygEditorFeatures : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Citations",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PostId = table.Column<int>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                PublicationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Publisher = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Citations", x => x.Id);
                table.ForeignKey(
                    name: "FK_Citations_Posts_PostId",
                    column: x => x.PostId,
                    principalTable: "Posts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Series",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Series", x => x.Id);
                table.ForeignKey(
                    name: "FK_Series_Users_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PostSeries",
            columns: table => new
            {
                PostId = table.Column<int>(type: "INTEGER", nullable: false),
                SeriesId = table.Column<int>(type: "INTEGER", nullable: false),
                OrderIndex = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PostSeries", x => new { x.PostId, x.SeriesId });
                table.ForeignKey(
                    name: "FK_PostSeries_Posts_PostId",
                    column: x => x.PostId,
                    principalTable: "Posts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PostSeries_Series_SeriesId",
                    column: x => x.SeriesId,
                    principalTable: "Series",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Citations_PostId",
            table: "Citations",
            column: "PostId");

        migrationBuilder.CreateIndex(
            name: "IX_PostSeries_SeriesId_OrderIndex",
            table: "PostSeries",
            columns: new[] { "SeriesId", "OrderIndex" });

        migrationBuilder.CreateIndex(
            name: "IX_Series_AuthorId",
            table: "Series",
            column: "AuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_Series_Name",
            table: "Series",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Citations");

        migrationBuilder.DropTable(
            name: "PostSeries");

        migrationBuilder.DropTable(
            name: "Series");
    }
}
