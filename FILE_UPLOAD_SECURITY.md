# Secure File Upload System

## Overview

This application implements a secure file upload system that follows security best practices by:

1. **Never storing uploaded files in wwwroot** - All files are stored outside the web root in `/storage`
2. **Database-tracked uploads** - All uploaded files are tracked in the database via `UploadedFile` entity
3. **Authorization-protected downloads** - Files are served through a dedicated endpoint that validates permissions
4. **Content validation** - Files are validated for type, size, and integrity (SHA256 hash)

## Architecture

### Storage Structure

```
/storage/
├── profile/
│   └── {user-guid}/
│       └── {file-guid}.{ext}
├── post/
│   ├── {post-id}/
│   │   ├── {file-guid}.{ext}
│   │   └── thumbnail.png
```

### Components

#### 1. UploadedFile Entity (`Data/UploadedFile.cs`)

Tracks all uploaded files in the database with:
- Unique GUID identifier
- Category (ProfileImage, PostAttachment, PostThumbnail)
- Original filename and content type
- Physical storage path
- File size and SHA256 hash
- Uploader and related post references
- Upload timestamp

#### 2. FileUploadService (`Services/FileUploadService.cs`)

Centralized service for all file upload operations:

**Methods:**
- `UploadProfileImageAsync(userId, file)` - Upload profile image (max 2MB)
- `UploadPostAttachmentAsync(postId, userId, file)` - Upload post attachment (max 10MB)
- `UploadPostThumbnailAsync(postId, userId, file)` - Upload post thumbnail (max 5MB)
- `GetFileByGuidAsync(guid)` - Retrieve file metadata by GUID
- `GetFileByIdAsync(id)` - Retrieve file metadata by ID
- `DeleteFileAsync(id)` - Delete file (both database record and physical file)
- `GetFilesByPostIdAsync(postId)` - Get all files for a post
- `GetProfileImageByUserIdAsync(userId)` - Get user's profile image

**Security Features:**
- File type validation (whitelist approach)
- File size limits per category
- SHA256 hash calculation for integrity verification
- Secure path generation (prevents path traversal)
- Automatic directory creation

#### 3. FileController (`Controllers/FileController.cs`)

Download endpoint that securely serves files:

**Endpoints:**
- `GET /file/download/{guid}` - Download file with original filename
- `GET /file/view/{guid}` - View file inline (for images)

**Authorization:**
- Profile images: Public access
- Post attachments/thumbnails: Requires post view permission
- Validates user permissions before serving files

### Usage in Controllers

#### Profile Image Upload (MyPageController)

```csharp
var uploadedFile = await _fileUploadService.UploadProfileImageAsync(userId, file);
var imageUrl = $"/file/view/{uploadedFile.FileGuid}";
await _userService.UpdateProfileAsync(userId, nickname, bio, contactEmail, imageUrl);
```

#### Post Attachment Upload (PostController)

```csharp
var uploadedFile = await _fileUploadService.UploadPostAttachmentAsync(postId, userId, file);
var fileUrl = $"/file/download/{uploadedFile.FileGuid}";
await _postService.AddAttachmentAsync(postId, fileName, fileUrl, contentType, size, hash);
```

## Security Benefits

### 1. No Direct Web Access
Files in `/storage` are not accessible via HTTP directly. All access goes through the FileController which validates permissions.

### 2. Authorization Enforcement
Every file download request is validated against:
- User authentication (where required)
- Post visibility rules
- User permissions

### 3. Path Traversal Prevention
- Files are stored using GUIDs, not user-provided names
- Physical paths are constructed server-side, never from user input
- All path operations use safe Path.Combine methods

### 4. Content Validation
- File type whitelist (no executable files)
- File size limits
- SHA256 hash for integrity verification
- Extension and MIME type validation

### 5. No Information Disclosure
- Original filenames are preserved only in database
- Physical paths use random GUIDs
- File metadata is only accessible to authorized users

## Migration from Old System

The previous vulnerable implementation stored files in `wwwroot/uploads`, making them:
- Directly accessible without authentication
- Vulnerable to path traversal attacks
- Not tracked in database
- Difficult to manage and audit

The new system:
- Stores all files in `/storage` (outside wwwroot)
- Tracks every upload in database
- Requires authorization for access
- Provides audit trail through UploadedFile records

## Database Migration

After stopping the application, create the migration:

```bash
cd Sources/PEngineV
dotnet ef migrations add AddUploadedFileTracking
dotnet ef database update
```

This will create the `UploadedFiles` table with proper indexes and foreign keys.

## Compliance

This implementation follows:
- OWASP Top 10 best practices for file upload security
- VIBE_RULE.md requirement #1 (Security First)
- Content Security Policy Level 3 (no inline content)
- Secure by default principles
