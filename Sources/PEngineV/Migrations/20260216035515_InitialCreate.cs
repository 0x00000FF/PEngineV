using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PEngineV.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Groups",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Groups", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Nickname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                ContactEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                Bio = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ProfileImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                PasswordSalt = table.Column<string>(type: "TEXT", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                TwoFactorSecret = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                ActionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Details = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuditLogs_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Posts",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                IsProtected = table.Column<bool>(type: "INTEGER", nullable: false),
                EncryptedContent = table.Column<string>(type: "TEXT", nullable: true),
                PasswordSalt = table.Column<string>(type: "TEXT", nullable: true),
                EncryptionIV = table.Column<string>(type: "TEXT", nullable: true),
                EncryptionTag = table.Column<string>(type: "TEXT", nullable: true),
                ThumbnailUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                PublishAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Posts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Posts_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Posts_Users_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserGroups",
            columns: table => new
            {
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                GroupId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                table.ForeignKey(
                    name: "FK_UserGroups_Groups_GroupId",
                    column: x => x.GroupId,
                    principalTable: "Groups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserGroups_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserPasskeys",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CredentialId = table.Column<string>(type: "TEXT", nullable: false),
                PublicKey = table.Column<string>(type: "TEXT", nullable: false),
                SignCount = table.Column<uint>(type: "INTEGER", nullable: false),
                UserHandle = table.Column<byte[]>(type: "BLOB", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPasskeys", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserPasskeys_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Attachments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PostId = table.Column<int>(type: "INTEGER", nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                StoredPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                Sha256Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                EncryptedData = table.Column<string>(type: "TEXT", nullable: true),
                EncryptionIV = table.Column<string>(type: "TEXT", nullable: true),
                EncryptionTag = table.Column<string>(type: "TEXT", nullable: true),
                UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Attachments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Attachments_Posts_PostId",
                    column: x => x.PostId,
                    principalTable: "Posts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Comments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PostId = table.Column<int>(type: "INTEGER", nullable: false),
                AuthorId = table.Column<int>(type: "INTEGER", nullable: true),
                ParentCommentId = table.Column<int>(type: "INTEGER", nullable: true),
                GuestName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                GuestEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                PasswordSalt = table.Column<string>(type: "TEXT", nullable: true),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Comments_Comments_ParentCommentId",
                    column: x => x.ParentCommentId,
                    principalTable: "Comments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Comments_Posts_PostId",
                    column: x => x.PostId,
                    principalTable: "Posts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Comments_Users_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "PostGroups",
            columns: table => new
            {
                PostId = table.Column<int>(type: "INTEGER", nullable: false),
                GroupId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PostGroups", x => new { x.PostId, x.GroupId });
                table.ForeignKey(
                    name: "FK_PostGroups_Groups_GroupId",
                    column: x => x.GroupId,
                    principalTable: "Groups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PostGroups_Posts_PostId",
                    column: x => x.PostId,
                    principalTable: "Posts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PostTags",
            columns: table => new
            {
                PostId = table.Column<int>(type: "INTEGER", nullable: false),
                TagId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PostTags", x => new { x.PostId, x.TagId });
                table.ForeignKey(
                    name: "FK_PostTags_Posts_PostId",
                    column: x => x.PostId,
                    principalTable: "Posts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PostTags_Tags_TagId",
                    column: x => x.TagId,
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Attachments_PostId",
            table: "Attachments",
            column: "PostId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId_Timestamp",
            table: "AuditLogs",
            columns: new[] { "UserId", "Timestamp" });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            table: "Categories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Comments_AuthorId",
            table: "Comments",
            column: "AuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_ParentCommentId",
            table: "Comments",
            column: "ParentCommentId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_PostId",
            table: "Comments",
            column: "PostId");

        migrationBuilder.CreateIndex(
            name: "IX_Groups_Name",
            table: "Groups",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PostGroups_GroupId",
            table: "PostGroups",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_Posts_AuthorId",
            table: "Posts",
            column: "AuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_Posts_CategoryId",
            table: "Posts",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Posts_PublishAt",
            table: "Posts",
            column: "PublishAt");

        migrationBuilder.CreateIndex(
            name: "IX_PostTags_TagId",
            table: "PostTags",
            column: "TagId");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Name",
            table: "Tags",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserGroups_GroupId",
            table: "UserGroups",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_UserPasskeys_CredentialId",
            table: "UserPasskeys",
            column: "CredentialId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserPasskeys_UserId",
            table: "UserPasskeys",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Attachments");

        migrationBuilder.DropTable(
            name: "AuditLogs");

        migrationBuilder.DropTable(
            name: "Comments");

        migrationBuilder.DropTable(
            name: "PostGroups");

        migrationBuilder.DropTable(
            name: "PostTags");

        migrationBuilder.DropTable(
            name: "UserGroups");

        migrationBuilder.DropTable(
            name: "UserPasskeys");

        migrationBuilder.DropTable(
            name: "Posts");

        migrationBuilder.DropTable(
            name: "Tags");

        migrationBuilder.DropTable(
            name: "Groups");

        migrationBuilder.DropTable(
            name: "Categories");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
