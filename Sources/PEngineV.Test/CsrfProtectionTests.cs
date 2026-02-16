using System.Net;
using System.Text;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PEngineV.Test;

[TestFixture]
public class CsrfProtectionTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Login_WithoutToken_ShouldReturn400()
    {
        // Act
        var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "testpass")
        }));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Login_WithValidToken_ShouldSucceed()
    {
        // Arrange - Get login page to extract CSRF token
        var getResponse = await _client.GetAsync("/Account/Login");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        Assert.That(token, Is.Not.Null.And.Not.Empty, "CSRF token should be present in login form");

        // Act - Submit login with token
        var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "wrongpass")
        }));

        // Assert - Should not be 400 (BadRequest for missing token)
        // Will be redirect or 200 with error message for wrong credentials
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_WithoutToken_ShouldReturn400()
    {
        // Act
        var response = await _client.PostAsync("/Account/Register", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "newuser"),
            new KeyValuePair<string, string>("Email", "test@test.com"),
            new KeyValuePair<string, string>("Password", "Password123!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Password123!")
        }));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_WithValidToken_ShouldNotReturn400()
    {
        // Arrange
        var getResponse = await _client.GetAsync("/Account/Register");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        Assert.That(token, Is.Not.Null.And.Not.Empty, "CSRF token should be present in register form");

        // Act
        var response = await _client.PostAsync("/Account/Register", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Username", "newuser"),
            new KeyValuePair<string, string>("Email", "test@test.com"),
            new KeyValuePair<string, string>("Password", "Password123!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Password123!")
        }));

        // Assert
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PostComment_WithoutToken_ShouldReturn400()
    {
        // Act - Try to post comment without token
        var response = await _client.PostAsync("/Post/Comment", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("PostId", "1"),
            new KeyValuePair<string, string>("Content", "Test comment"),
            new KeyValuePair<string, string>("Name", "Test User"),
            new KeyValuePair<string, string>("Password", "test123")
        }));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GuestbookWrite_WithoutToken_ShouldReturn400()
    {
        // Act
        var response = await _client.PostAsync("/Guestbook/Write", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Name", "Test User"),
            new KeyValuePair<string, string>("Message", "Test message")
        }));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task AjaxRequest_WithTokenInHeader_ShouldWork()
    {
        // Arrange - Get a page with token
        var getResponse = await _client.GetAsync("/Account/Login");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        Assert.That(token, Is.Not.Null.And.Not.Empty);

        // Act - Make AJAX-style request with token in header
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { Username = "test", Password = "test" }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("RequestVerificationToken", token);

        var response = await _client.SendAsync(request);

        // Assert - Should not be rejected for missing token
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetRequests_ShouldNotRequireToken()
    {
        // Act - GET requests should work without tokens
        var homeResponse = await _client.GetAsync("/");
        var loginResponse = await _client.GetAsync("/Account/Login");
        var registerResponse = await _client.GetAsync("/Account/Register");

        // Assert - All should succeed
        Assert.That(homeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task FileDownload_ShouldNotRequireToken()
    {
        // Act - File download is GET and should not require token
        var response = await _client.GetAsync("/file/download/00000000-0000-0000-0000-000000000000");

        // Assert - Should be NotFound (file doesn't exist) not BadRequest (missing token)
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AllPublicForms_ShouldHaveAntiForgeryToken()
    {
        // Arrange - Pages with forms that should have tokens
        var pagesToCheck = new[]
        {
            "/Account/Login",
            "/Account/Register"
        };

        foreach (var page in pagesToCheck)
        {
            // Act
            var response = await _client.GetAsync(page);
            var html = await response.Content.ReadAsStringAsync();
            var token = ExtractAntiForgeryToken(html);

            // Assert
            Assert.That(token, Is.Not.Null.And.Not.Empty,
                $"Page {page} should have antiforgery token in form");
        }
    }

    [Test]
    public async Task InvalidToken_ShouldReturn400()
    {
        // Act - Try to login with invalid/fake token
        var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", "FAKE_INVALID_TOKEN"),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "testpass")
        }));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var tokenInput = document.QuerySelector("input[name='__RequestVerificationToken']");
        return tokenInput?.GetAttribute("value") ?? string.Empty;
    }
}
