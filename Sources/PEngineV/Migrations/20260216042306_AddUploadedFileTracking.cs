using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PEngineV.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedFileTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileGuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UploadedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    RelatedPostId = table.Column<int>(type: "INTEGER", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_Posts_RelatedPostId",
                        column: x => x.RelatedPostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_Category_RelatedPostId",
                table: "UploadedFiles",
                columns: new[] { "Category", "RelatedPostId" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_FileGuid",
                table: "UploadedFiles",
                column: "FileGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_RelatedPostId",
                table: "UploadedFiles",
                column: "RelatedPostId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UploadedByUserId",
                table: "UploadedFiles",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadedFiles");
        }
    }
}
