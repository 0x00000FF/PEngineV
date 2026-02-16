using Microsoft.EntityFrameworkCore;

namespace PEngineV.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPasskey> UserPasskeys => Set<UserPasskey>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<PostGroup> PostGroups => Set<PostGroup>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<Citation> Citations => Set<Citation>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<PostSeries> PostSeries => Set<PostSeries>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<UserPasskey>(entity =>
        {
            entity.HasIndex(p => p.CredentialId).IsUnique();
            entity.HasOne(p => p.User)
                .WithMany(u => u.Passkeys)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.Timestamp });
            entity.HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(p => p.PublishAt);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<PostTag>(entity =>
        {
            entity.HasKey(pt => new { pt.PostId, pt.TagId });
            entity.HasOne(pt => pt.Post)
                .WithMany(p => p.PostTags)
                .HasForeignKey(pt => pt.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pt => pt.Tag)
                .WithMany(t => t.PostTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasOne(a => a.Post)
                .WithMany(p => p.Attachments)
                .HasForeignKey(a => a.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(g => g.Name).IsUnique();
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.HasKey(ug => new { ug.UserId, ug.GroupId });
            entity.HasOne(ug => ug.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(ug => ug.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostGroup>(entity =>
        {
            entity.HasKey(pg => new { pg.PostId, pg.GroupId });
            entity.HasOne(pg => pg.Post)
                .WithMany(p => p.PostGroups)
                .HasForeignKey(pg => pg.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pg => pg.Group)
                .WithMany(g => g.PostGroups)
                .HasForeignKey(pg => pg.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UploadedFile>(entity =>
        {
            entity.HasIndex(uf => uf.FileGuid).IsUnique();
            entity.HasIndex(uf => new { uf.Category, uf.RelatedPostId });
            entity.HasIndex(uf => uf.UploadedByUserId);
            entity.HasOne(uf => uf.UploadedBy)
                .WithMany()
                .HasForeignKey(uf => uf.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(uf => uf.RelatedPost)
                .WithMany()
                .HasForeignKey(uf => uf.RelatedPostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Citation>(entity =>
        {
            entity.HasOne(c => c.Post)
                .WithMany(p => p.Citations)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(c => c.PostId);
        });

        modelBuilder.Entity<Series>(entity =>
        {
            entity.HasIndex(s => s.Name).IsUnique();
            entity.HasOne(s => s.Author)
                .WithMany()
                .HasForeignKey(s => s.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostSeries>(entity =>
        {
            entity.HasKey(ps => new { ps.PostId, ps.SeriesId });
            entity.HasOne(ps => ps.Post)
                .WithMany(p => p.PostSeries)
                .HasForeignKey(ps => ps.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ps => ps.Series)
                .WithMany(s => s.PostSeries)
                .HasForeignKey(ps => ps.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(ps => new { ps.SeriesId, ps.OrderIndex });
        });
    }
}
