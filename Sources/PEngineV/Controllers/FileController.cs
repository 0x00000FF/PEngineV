using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using PEngineV.Data;
using PEngineV.Services;

namespace PEngineV.Controllers;

[AutoValidateAntiforgeryToken]
public class FileController : Controller
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IPostService _postService;

    public FileController(IFileUploadService fileUploadService, IPostService postService)
    {
        _fileUploadService = fileUploadService;
        _postService = postService;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? int.Parse(claim.Value) : null;
    }

    [HttpGet("/file/download/{guid}")]
    public async Task<IActionResult> Download(Guid guid)
    {
        var file = await _fileUploadService.GetFileByGuidAsync(guid);
        if (file is null)
        {
            return NotFound();
        }

        if (!await CanUserAccessFileAsync(file))
        {
            return Forbid();
        }

        var physicalPath = _fileUploadService.GetPhysicalPath(file);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        return PhysicalFile(physicalPath, file.ContentType, file.OriginalFileName);
    }

    [HttpGet("/file/view/{guid}")]
    public async Task<IActionResult> View(Guid guid)
    {
        var file = await _fileUploadService.GetFileByGuidAsync(guid);
        if (file is null)
        {
            return NotFound();
        }

        if (!await CanUserAccessFileAsync(file))
        {
            return Forbid();
        }

        var physicalPath = _fileUploadService.GetPhysicalPath(file);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        return PhysicalFile(physicalPath, file.ContentType);
    }

    private async Task<bool> CanUserAccessFileAsync(UploadedFile file)
    {
        var userId = GetCurrentUserId();

        switch (file.Category)
        {
            case FileCategory.ProfileImage:
                return true;

            case FileCategory.PostAttachment:
            case FileCategory.PostThumbnail:
                if (file.RelatedPostId is null)
                {
                    return false;
                }

                return await _postService.CanUserViewPostAsync(file.RelatedPostId.Value, userId);

            default:
                return false;
        }
    }
}
