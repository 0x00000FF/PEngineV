using Microsoft.EntityFrameworkCore;
using PEngineV.Data;
using PEngineV.Services;

namespace PEngineV.Test;

[TestFixture]
public class PostServiceTests
{
    private AppDbContext _db = null!;
    private PostService _service = null!;
    private IPasswordHasher _hasher = null!;
    private IEncryptionService _encryption = null!;
    private User _testUser = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();

        _hasher = new Pbkdf2PasswordHasher();
        _encryption = new AesGcmEncryptionService();
        _service = new PostService(_db, _encryption, _hasher);

        _testUser = new User
        {
            Username = "testuser",
            Nickname = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };
        _db.Users.Add(_testUser);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.CloseConnectionAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task CreatePost_And_GetById()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "Test Title", "Test Content",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);

        var retrieved = await _service.GetPostByIdAsync(post.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Title, Is.EqualTo("Test Title"));
        Assert.That(retrieved.Content, Is.EqualTo("Test Content"));
    }

    [Test]
    public async Task CreatePost_With_Tags()
    {
        var tags = new[] { "csharp", "dotnet" };
        var post = await _service.CreatePostAsync(_testUser.Id, "Tagged Post", "Content",
            null, PostVisibility.Public, null, null, tags, null);

        var retrieved = await _service.GetPostByIdAsync(post.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.PostTags, Has.Count.EqualTo(2));
        var tagNames = retrieved.PostTags.Select(pt => pt.Tag.Name).OrderBy(n => n).ToList();
        Assert.That(tagNames, Is.EqualTo(new[] { "csharp", "dotnet" }));
    }

    [Test]
    public async Task CreatePost_Protected()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "Protected Post", "Secret Content",
            null, PostVisibility.Public, "mypassword", null, Array.Empty<string>(), null);

        var retrieved = await _service.GetPostByIdAsync(post.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.IsProtected, Is.True);
        Assert.That(retrieved.Content, Is.EqualTo(""));
        Assert.That(retrieved.EncryptedContent, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task DecryptPostContent_With_Correct_Password()
    {
        const string content = "Secret Content Here";
        const string password = "mypassword";

        var post = await _service.CreatePostAsync(_testUser.Id, "Protected", content,
            null, PostVisibility.Public, password, null, Array.Empty<string>(), null);

        var decrypted = await _service.DecryptPostContentAsync(post.Id, password);

        Assert.That(decrypted, Is.EqualTo(content));
    }

    [Test]
    public async Task DecryptPostContent_With_Wrong_Password()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "Protected", "Secret",
            null, PostVisibility.Public, "correctpw", null, Array.Empty<string>(), null);

        var decrypted = await _service.DecryptPostContentAsync(post.Id, "wrongpw");

        Assert.That(decrypted, Is.Null);
    }

    [Test]
    public async Task GetPublishedPosts_Excludes_Future_Posts()
    {
        await _service.CreatePostAsync(_testUser.Id, "Past Post", "Content",
            null, PostVisibility.Public, null, DateTime.UtcNow.AddDays(-1), Array.Empty<string>(), null);
        await _service.CreatePostAsync(_testUser.Id, "Future Post", "Content",
            null, PostVisibility.Public, null, DateTime.UtcNow.AddDays(1), Array.Empty<string>(), null);

        var posts = await _service.GetPublishedPostsAsync(null);

        Assert.That(posts, Has.Count.EqualTo(1));
        Assert.That(posts[0].Title, Is.EqualTo("Past Post"));
    }

    [Test]
    public async Task GetPublishedPosts_Anonymous_Only_Public()
    {
        await _service.CreatePostAsync(_testUser.Id, "Public Post", "Content",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);
        await _service.CreatePostAsync(_testUser.Id, "Internal Post", "Content",
            null, PostVisibility.Internal, null, null, Array.Empty<string>(), null);
        await _service.CreatePostAsync(_testUser.Id, "Private Post", "Content",
            null, PostVisibility.Private, null, null, Array.Empty<string>(), null);

        var posts = await _service.GetPublishedPostsAsync(null);

        Assert.That(posts, Has.Count.EqualTo(1));
        Assert.That(posts[0].Title, Is.EqualTo("Public Post"));
    }

    [Test]
    public async Task CreateComment_And_GetComments()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "Post", "Content",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);

        await _service.CreateCommentAsync(post.Id, _testUser.Id, null, null, null, null,
            "First comment", false);
        await _service.CreateCommentAsync(post.Id, _testUser.Id, null, null, null, null,
            "Second comment", false);

        var comments = await _service.GetCommentsAsync(post.Id);

        Assert.That(comments, Has.Count.EqualTo(2));
        Assert.That(comments[0].Content, Is.EqualTo("First comment"));
        Assert.That(comments[1].Content, Is.EqualTo("Second comment"));
    }

    [Test]
    public async Task CreateComment_Private()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "Post", "Content",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);

        var comment = await _service.CreateCommentAsync(post.Id, null, null,
            "Guest", "guest@test.com", "guestpw", "Private comment", true);

        Assert.That(comment.IsPrivate, Is.True);
        Assert.That(comment.GuestName, Is.EqualTo("Guest"));
        Assert.That(comment.PasswordHash, Is.Not.Null);
        Assert.That(comment.PasswordSalt, Is.Not.Null);
    }

    [Test]
    public async Task VerifyCommentPassword()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "Post", "Content",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);

        var comment = await _service.CreateCommentAsync(post.Id, null, null,
            "Guest", "guest@test.com", "secretpw", "Comment", false);

        Assert.That(await _service.VerifyCommentPasswordAsync(comment.Id, "secretpw"), Is.True);
        Assert.That(await _service.VerifyCommentPasswordAsync(comment.Id, "wrongpw"), Is.False);
    }

    [Test]
    public async Task Search_Finds_Posts_By_Title()
    {
        await _service.CreatePostAsync(_testUser.Id, "Unique Alpha Title", "Body text",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);
        await _service.CreatePostAsync(_testUser.Id, "Other Post", "Other body",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);

        var (posts, _) = await _service.SearchAsync("Alpha", null);

        Assert.That(posts, Has.Count.EqualTo(1));
        Assert.That(posts[0].Title, Is.EqualTo("Unique Alpha Title"));
    }

    [Test]
    public async Task Search_Finds_Comments_By_Content()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "Post", "Content",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);
        await _service.CreateCommentAsync(post.Id, _testUser.Id, null, null, null, null,
            "This has a unique keyword xylophone", false);

        var (_, comments) = await _service.SearchAsync("xylophone", null);

        Assert.That(comments, Has.Count.EqualTo(1));
        Assert.That(comments[0].Content, Does.Contain("xylophone"));
    }

    [Test]
    public async Task GetOrCreateCategory_Creates_New()
    {
        var category = await _service.GetOrCreateCategoryAsync("New Category");

        Assert.That(category, Is.Not.Null);
        Assert.That(category.Name, Is.EqualTo("New Category"));
        Assert.That(category.Id, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetOrCreateCategory_Returns_Existing()
    {
        var first = await _service.GetOrCreateCategoryAsync("Existing");
        var second = await _service.GetOrCreateCategoryAsync("Existing");

        Assert.That(second.Id, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task DeletePost_Removes_Post()
    {
        var post = await _service.CreatePostAsync(_testUser.Id, "To Delete", "Content",
            null, PostVisibility.Public, null, null, Array.Empty<string>(), null);

        await _service.DeletePostAsync(post.Id);

        var retrieved = await _service.GetPostByIdAsync(post.Id);
        Assert.That(retrieved, Is.Null);
    }
}
