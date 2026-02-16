using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PEngineV.Controllers;
using PEngineV.Data;
using PEngineV.Services;

namespace PEngineV.Test;

[TestFixture]
public class WysiwygEditorTests
{
    private AppDbContext _context = null!;
#pragma warning disable NUnit1032
    private PostController? _controller;
#pragma warning restore NUnit1032
    private IPostService _postService = null!;
    private IFileUploadService _fileUploadService = null!;
    private IEncryptionService _encryptionService = null!;
    private IPasswordHasher _passwordHasher = null!;
    private User _testUser = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // Create test user
        _testUser = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            Nickname = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        // Mock services
        _encryptionService = new MockEncryptionService();
        _passwordHasher = new MockPasswordHasher();
        _postService = new PostService(_context, _encryptionService, _passwordHasher);
        _fileUploadService = new MockFileUploadService();

        // Create controller
        _controller = new PostController(_postService, _fileUploadService, _context);

        // Set up fake user context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUser.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task Write_WithCitations_SavesCitationsToDatabase()
    {
        // Arrange
        var citations = new[]
        {
            new { title = "Test Citation 1", author = "Author 1", url = "https://example.com/1", date = "2024-01-01", publisher = "Publisher 1", notes = "Notes 1" },
            new { title = "Test Citation 2", author = "Author 2", url = "https://example.com/2", date = "2024-01-02", publisher = "Publisher 2", notes = "Notes 2" }
        };
        var citationsJson = JsonSerializer.Serialize(citations);

        // Act
        var result = await _controller.Write(
            title: "Test Post",
            content: "<p>Test content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: citationsJson,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var savedCitations = await _context.Citations.ToListAsync();
        Assert.That(savedCitations, Has.Count.EqualTo(2));
        Assert.That(savedCitations[0].Title, Is.EqualTo("Test Citation 1"));
        Assert.That(savedCitations[0].Author, Is.EqualTo("Author 1"));
        Assert.That(savedCitations[0].Url, Is.EqualTo("https://example.com/1"));
        Assert.That(savedCitations[0].OrderIndex, Is.EqualTo(0));
        Assert.That(savedCitations[1].Title, Is.EqualTo("Test Citation 2"));
        Assert.That(savedCitations[1].OrderIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task Write_WithSeries_SavesPostSeriesRelationship()
    {
        // Arrange
        var series = new Series
        {
            Name = "Test Series",
            Description = "Test Description",
            AuthorId = _testUser.Id
        };
        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Write(
            title: "Test Post",
            content: "<p>Test content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: series.Id,
            seriesOrder: 1
        );

        // Assert
        var postSeries = await _context.PostSeries.FirstOrDefaultAsync();
        Assert.That(postSeries, Is.Not.Null);
        Assert.That(postSeries.SeriesId, Is.EqualTo(series.Id));
        Assert.That(postSeries.OrderIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task Edit_WithCitations_UpdatesCitations()
    {
        // Arrange - Create initial post with citations
        var post = await _postService.CreatePostAsync(
            _testUser.Id, "Test Post", "<p>Content</p>", null,
            PostVisibility.Public, null, null, Enumerable.Empty<string>(), null);

        var oldCitation = new Citation
        {
            PostId = post.Id,
            Title = "Old Citation",
            OrderIndex = 0
        };
        _context.Citations.Add(oldCitation);
        await _context.SaveChangesAsync();

        // New citations for update
        var newCitations = new[]
        {
            new { title = "New Citation 1", author = "New Author", url = (string?)null, date = (string?)null, publisher = (string?)null, notes = (string?)null }
        };
        var citationsJson = JsonSerializer.Serialize(newCitations);

        // Act
        var result = await _controller.Edit(
            id: post.Id,
            title: "Updated Post",
            content: "<p>Updated content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: citationsJson,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var citations = await _context.Citations.Where(c => c.PostId == post.Id).ToListAsync();
        Assert.That(citations, Has.Count.EqualTo(1));
        Assert.That(citations[0].Title, Is.EqualTo("New Citation 1"));
        Assert.That(citations[0].Author, Is.EqualTo("New Author"));
    }

    [Test]
    public async Task Edit_WithSeries_UpdatesSeriesRelationship()
    {
        // Arrange
        var series1 = new Series { Name = "Series 1", AuthorId = _testUser.Id };
        var series2 = new Series { Name = "Series 2", AuthorId = _testUser.Id };
        _context.Series.AddRange(series1, series2);
        await _context.SaveChangesAsync();

        var post = await _postService.CreatePostAsync(
            _testUser.Id, "Test Post", "<p>Content</p>", null,
            PostVisibility.Public, null, null, Enumerable.Empty<string>(), null);

        var oldPostSeries = new PostSeries
        {
            PostId = post.Id,
            SeriesId = series1.Id,
            OrderIndex = 0
        };
        _context.PostSeries.Add(oldPostSeries);
        await _context.SaveChangesAsync();

        // Act - Change to series2
        var result = await _controller.Edit(
            id: post.Id,
            title: "Updated Post",
            content: "<p>Updated content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: series2.Id,
            seriesOrder: 2
        );

        // Assert
        var postSeriesList = await _context.PostSeries.Where(ps => ps.PostId == post.Id).ToListAsync();
        Assert.That(postSeriesList, Has.Count.EqualTo(1));
        Assert.That(postSeriesList[0].SeriesId, Is.EqualTo(series2.Id));
        Assert.That(postSeriesList[0].OrderIndex, Is.EqualTo(2));
    }

    [Test]
    public async Task Write_WithEmptyCitationsJson_DoesNotCreateCitations()
    {
        // Act
        var result = await _controller.Write(
            title: "Test Post",
            content: "<p>Test content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: "",
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var citations = await _context.Citations.ToListAsync();
        Assert.That(citations, Is.Empty);
    }

    [Test]
    public async Task Write_WithNullCitationsJson_DoesNotCreateCitations()
    {
        // Act
        var result = await _controller.Write(
            title: "Test Post",
            content: "<p>Test content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var citations = await _context.Citations.ToListAsync();
        Assert.That(citations, Is.Empty);
    }

    [Test]
    public async Task Write_WithInvalidCitationsJson_DoesNotThrowException()
    {
        // Act & Assert - Should not throw
        Assert.DoesNotThrowAsync(async () =>
        {
            await _controller.Write(
                title: "Test Post",
                content: "<p>Test content</p>",
                categoryName: null,
                tags: null,
                visibility: "Public",
                password: null,
                publishAt: null,
                files: null,
                citationsJson: "invalid json {{{",
                seriesId: null,
                seriesOrder: 0
            );
        });

        var citations = await _context.Citations.ToListAsync();
        Assert.That(citations, Is.Empty);
    }

    [Test]
    public async Task Edit_RemovingAllCitations_DeletesAllCitations()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(
            _testUser.Id, "Test Post", "<p>Content</p>", null,
            PostVisibility.Public, null, null, Enumerable.Empty<string>(), null);

        _context.Citations.Add(new Citation { PostId = post.Id, Title = "Citation 1", OrderIndex = 0 });
        _context.Citations.Add(new Citation { PostId = post.Id, Title = "Citation 2", OrderIndex = 1 });
        await _context.SaveChangesAsync();

        // Act - Update with no citations
        await _controller.Edit(
            id: post.Id,
            title: "Updated Post",
            content: "<p>Updated content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var citations = await _context.Citations.Where(c => c.PostId == post.Id).ToListAsync();
        Assert.That(citations, Is.Empty);
    }

    [Test]
    public async Task Edit_RemovingSeries_DeletesSeriesRelationship()
    {
        // Arrange
        var series = new Series { Name = "Test Series", AuthorId = _testUser.Id };
        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        var post = await _postService.CreatePostAsync(
            _testUser.Id, "Test Post", "<p>Content</p>", null,
            PostVisibility.Public, null, null, Enumerable.Empty<string>(), null);

        _context.PostSeries.Add(new PostSeries { PostId = post.Id, SeriesId = series.Id, OrderIndex = 0 });
        await _context.SaveChangesAsync();

        // Act - Update with no series
        await _controller.Edit(
            id: post.Id,
            title: "Updated Post",
            content: "<p>Updated content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var postSeries = await _context.PostSeries.Where(ps => ps.PostId == post.Id).ToListAsync();
        Assert.That(postSeries, Is.Empty);
    }

    [Test]
    public async Task Citations_MaintainOrderIndex()
    {
        // Arrange
        var citations = new[]
        {
            new { title = "Third", author = (string?)null, url = (string?)null, date = (string?)null, publisher = (string?)null, notes = (string?)null },
            new { title = "First", author = (string?)null, url = (string?)null, date = (string?)null, publisher = (string?)null, notes = (string?)null },
            new { title = "Second", author = (string?)null, url = (string?)null, date = (string?)null, publisher = (string?)null, notes = (string?)null }
        };
        var citationsJson = JsonSerializer.Serialize(citations);

        // Act
        await _controller.Write(
            title: "Test Post",
            content: "<p>Test content</p>",
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: citationsJson,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var savedCitations = await _context.Citations.OrderBy(c => c.OrderIndex).ToListAsync();
        Assert.That(savedCitations[0].Title, Is.EqualTo("Third"));
        Assert.That(savedCitations[0].OrderIndex, Is.EqualTo(0));
        Assert.That(savedCitations[1].Title, Is.EqualTo("First"));
        Assert.That(savedCitations[1].OrderIndex, Is.EqualTo(1));
        Assert.That(savedCitations[2].Title, Is.EqualTo("Second"));
        Assert.That(savedCitations[2].OrderIndex, Is.EqualTo(2));
    }

    [Test]
    public async Task Write_WithFontSizeClasses_SavesContentCorrectly()
    {
        // Arrange
        var contentWithFontSizes = "<p>Normal text <span class=\"pe-font-size-xs\">Extra small</span> " +
                                   "<span class=\"pe-font-size-sm\">Small</span> " +
                                   "<span class=\"pe-font-size-lg\">Large</span> " +
                                   "<span class=\"pe-font-size-xl\">Extra large</span> " +
                                   "<span class=\"pe-font-size-xxl\">XX Large</span></p>";

        // Act
        var result = await _controller.Write(
            title: "Test Post with Font Sizes",
            content: contentWithFontSizes,
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var savedPost = await _context.Posts.FirstOrDefaultAsync();
        Assert.That(savedPost, Is.Not.Null);
        Assert.That(savedPost.Content, Does.Contain("pe-font-size-xs"));
        Assert.That(savedPost.Content, Does.Contain("pe-font-size-sm"));
        Assert.That(savedPost.Content, Does.Contain("pe-font-size-lg"));
        Assert.That(savedPost.Content, Does.Contain("pe-font-size-xl"));
        Assert.That(savedPost.Content, Does.Contain("pe-font-size-xxl"));
    }

    [Test]
    public async Task Edit_WithFontSizeClasses_UpdatesContentCorrectly()
    {
        // Arrange
        var post = await _postService.CreatePostAsync(
            _testUser.Id, "Test Post", "<p>Original content</p>", null,
            PostVisibility.Public, null, null, Enumerable.Empty<string>(), null);

        var updatedContent = "<p>Updated with <span class=\"pe-font-size-xl\">large text</span></p>";

        // Act
        var result = await _controller.Edit(
            id: post.Id,
            title: "Updated Post",
            content: updatedContent,
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var updatedPost = await _context.Posts.FindAsync(post.Id);
        Assert.That(updatedPost, Is.Not.Null);
        Assert.That(updatedPost.Content, Does.Contain("pe-font-size-xl"));
        Assert.That(updatedPost.Content, Does.Contain("large text"));
    }

    [Test]
    public async Task Write_WithNestedFontSizeAndColorClasses_SavesContentCorrectly()
    {
        // Arrange - Test that font size works with other WYSIWYG features
        var contentWithMixedFormatting = "<p><span class=\"pe-font-size-lg\">" +
                                        "<span class=\"pe-color-fg-11\">Large red text</span></span></p>";

        // Act
        var result = await _controller.Write(
            title: "Test Post with Mixed Formatting",
            content: contentWithMixedFormatting,
            categoryName: null,
            tags: null,
            visibility: "Public",
            password: null,
            publishAt: null,
            files: null,
            citationsJson: null,
            seriesId: null,
            seriesOrder: 0
        );

        // Assert
        var savedPost = await _context.Posts.FirstOrDefaultAsync();
        Assert.That(savedPost, Is.Not.Null);
        Assert.That(savedPost.Content, Does.Contain("pe-font-size-lg"));
        Assert.That(savedPost.Content, Does.Contain("pe-color-fg-11"));
        Assert.That(savedPost.Content, Does.Contain("Large red text"));
    }
}

// Mock services for testing
public class MockFileUploadService : IFileUploadService
{
    public Task<UploadedFile> UploadProfileImageAsync(int userId, IFormFile file) => throw new NotImplementedException();
    public Task<UploadedFile> UploadPostAttachmentAsync(int postId, int userId, IFormFile file) => throw new NotImplementedException();
    public Task<UploadedFile> UploadPostThumbnailAsync(int postId, int userId, IFormFile file) => throw new NotImplementedException();
    public Task<IEnumerable<UploadedFile>> GetFilesByPostIdAsync(int postId) => Task.FromResult(Enumerable.Empty<UploadedFile>());
    public Task<UploadedFile?> GetFileByGuidAsync(Guid fileGuid) => throw new NotImplementedException();
    public Task<UploadedFile?> GetFileByIdAsync(int fileId) => throw new NotImplementedException();
    public Task<bool> DeleteFileAsync(int fileId) => throw new NotImplementedException();
    public Task<UploadedFile?> GetProfileImageByUserIdAsync(int userId) => throw new NotImplementedException();
    public string GetPhysicalPath(UploadedFile file) => throw new NotImplementedException();
}

public class MockEncryptionService : IEncryptionService
{
    public EncryptionResult Encrypt(string plaintext, string password)
    {
        return new EncryptionResult("encrypted", "salt", "iv", "tag");
    }

    public string Decrypt(string encryptedData, string password, string salt, string iv, string tag)
    {
        return "decrypted";
    }

    public ByteEncryptionResult EncryptBytes(byte[] data, string password, string salt)
    {
        return new ByteEncryptionResult(data, salt, "iv", "tag");
    }

    public byte[] DecryptBytes(byte[] encryptedData, string password, string salt, string iv, string tag)
    {
        return encryptedData;
    }
}

public class MockPasswordHasher : IPasswordHasher
{
    public (string hash, string salt) HashPassword(string password)
    {
        return ("mockhash", "mocksalt");
    }

    public bool VerifyPassword(string password, string hash, string salt) => true;
}
