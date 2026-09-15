using NexaECommerce.Server.Features.Appearance;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Uploads;

public sealed class UploadEndpoints : IFeatureEndpoints
{
    private const long MaxLogoSize = 5 * 1024 * 1024;

    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/uploads")
                .WithTags("Uploads")
                .AddEndpointFilter<ValidationFilter>()
                .AddEndpointFilter<PerformanceFilter>();

        // --------------------------------------------------------
        // Generic image upload
        // --------------------------------------------------------

        group.MapPost(
                "/",
                UploadFile)
            .RequireAuthorization()
            .DisableAntiforgery();

        // --------------------------------------------------------
        // Dedicated store logo upload
        // --------------------------------------------------------

        group.MapPost(
                "/logo",
                UploadLogo)
            .RequireAuthorization()
            .RequirePermission(
                AppearancePermissions.Manage)
            .DisableAntiforgery();

        // --------------------------------------------------------
        // Existing delete endpoint
        // --------------------------------------------------------

        group.MapDelete(
                "/{fileName}",
                DeleteFile)
            .AllowAnonymous()
            .DisableAntiforgery();
    }

    private static async Task<IResult> UploadFile(
        IFormFile file,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(
                new
                {
                    error = "No file was selected."
                });
        }

        var allowedTypes =
            new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif",
                "image/svg+xml"
            };

        if (!allowedTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Invalid file type. Only image files are allowed."
                });
        }

        if (file.Length > MaxLogoSize)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "File size cannot exceed 5 MB."
                });
        }

        try
        {
            var uploadPath =
                GetUploadPath(env);

            Directory.CreateDirectory(
                uploadPath);

            var extension =
                Path.GetExtension(
                    file.FileName);

            if (string.IsNullOrWhiteSpace(
                    extension))
            {
                extension =
                    GetExtensionFromContentType(
                        file.ContentType);
            }

            extension =
                NormalizeExtension(
                    extension);

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    uploadPath,
                    fileName);

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    useAsync: true);

            await file.CopyToAsync(
                stream,
                ct);

            return Results.Ok(
                new
                {
                    url =
                        $"/uploads/{fileName}",
                    fileName,
                    size = file.Length,
                    contentType =
                        file.ContentType,
                    success = true
                });
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Results.StatusCode(500);
        }
    }

    private static async Task<IResult> UploadLogo(
        IFormFile file,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(
                new
                {
                    error = "No logo file was selected."
                });
        }

        if (file.Length > MaxLogoSize)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Logo size cannot exceed 5 MB."
                });
        }

        var allowedTypes =
            new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif"
            };

        if (!allowedTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Logo must be JPG, PNG, WEBP or GIF."
                });
        }

        try
        {
            var detectedType =
                await DetectImageTypeAsync(
                    file,
                    ct);

            if (detectedType is null)
            {
                return Results.BadRequest(
                    new
                    {
                        error =
                            "The uploaded file is not a valid supported image."
                    });
            }

            if (!string.Equals(
                    file.ContentType,
                    detectedType.Value.ContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(
                    new
                    {
                        error =
                            "The uploaded file content does not match its declared image type."
                    });
            }

            var uploadPath =
                GetUploadPath(env);

            Directory.CreateDirectory(
                uploadPath);

            var fileName =
                $"{Guid.NewGuid():N}{detectedType.Value.Extension}";

            var filePath =
                Path.Combine(
                    uploadPath,
                    fileName);

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    useAsync: true);

            await file.CopyToAsync(
                stream,
                ct);

            return Results.Ok(
                new
                {
                    url =
                        $"/uploads/{fileName}",
                    fileName,
                    size = file.Length,
                    contentType =
                        detectedType.Value.ContentType,
                    success = true
                });
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Results.StatusCode(500);
        }
    }

    private static async Task<ImageType?> DetectImageTypeAsync(
        IFormFile file,
        CancellationToken ct)
    {
        var header = new byte[12];

        if (file.Length > MaxLogoSize)
        {
            throw new InvalidOperationException("File exceeds maximum allowed size.");
        }

        await using var stream = file.OpenReadStream();

        var offset = 0;

        while (offset < header.Length)
        {
            var read =
                await stream.ReadAsync(
                    header.AsMemory(offset),
                    ct);

            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        // PNG
        if (offset >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return new ImageType(
                ".png",
                "image/png");
        }

        // JPEG
        if (offset >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return new ImageType(
                ".jpg",
                "image/jpeg");
        }

        // GIF
        if (offset >= 6 &&
            header[0] == (byte)'G' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'8' &&
            (header[4] == (byte)'7' ||
             header[4] == (byte)'9') &&
            header[5] == (byte)'a')
        {
            return new ImageType(
                ".gif",
                "image/gif");
        }

        // WEBP
        if (offset >= 12 &&
            header[0] == (byte)'R' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'F' &&
            header[8] == (byte)'W' &&
            header[9] == (byte)'E' &&
            header[10] == (byte)'B' &&
            header[11] == (byte)'P')
        {
            return new ImageType(
                ".webp",
                "image/webp");
        }

        return null;
    }

    private static string GetUploadPath(
        IWebHostEnvironment env)
    {
        var webRoot =
            string.IsNullOrWhiteSpace(
                env.WebRootPath)
                ? Path.Combine(
                    env.ContentRootPath,
                    "wwwroot")
                : env.WebRootPath;

        return Path.Combine(
            webRoot,
            "uploads");
    }

    private static string GetExtensionFromContentType(
        string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/svg+xml" => ".svg",
            _ => ".bin"
        };
    }

    private static string NormalizeExtension(
        string extension)
    {
        var normalized =
            extension.Trim().ToLowerInvariant();

        return normalized switch
        {
            ".jpeg" => ".jpg",
            ".jpg" => ".jpg",
            ".png" => ".png",
            ".webp" => ".webp",
            ".gif" => ".gif",
            ".svg" => ".svg",
            _ => ".bin"
        };
    }

    private static async Task<IResult> DeleteFile(
        string fileName,
        IWebHostEnvironment env)
    {
        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "File name is required."
                });
        }

        // Prevent path traversal.
        var safeFileName =
            Path.GetFileName(
                fileName);

        if (!string.Equals(
                safeFileName,
                fileName,
                StringComparison.Ordinal))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Invalid file name."
                });
        }

        var uploadPath =
            GetUploadPath(env);

        var filePath =
            Path.Combine(
                uploadPath,
                safeFileName);

        if (!File.Exists(filePath))
        {
            return Results.NotFound(
                new
                {
                    error =
                        "File not found."
                });
        }

        File.Delete(filePath);

        return Results.Ok(
            new
            {
                message =
                    "File deleted successfully."
            });
    }

    private readonly record struct ImageType(
        string Extension,
        string ContentType);
}