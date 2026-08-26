// NexaECommerce.Server/Features/Uploads/UploadEndpoints.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaECommerce.Server.Platform.Authorization;
namespace NexaECommerce.Server.Features.Uploads;

public sealed class UploadEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/uploads")
            .WithTags("Uploads")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        // ✅ غیرفعال کردن Anti-Forgery برای آپلود
        group.MapPost("/", UploadFile)
              .RequireAuthorization()
             .DisableAntiforgery(); // ✅ این خط را اضافه کنید

        group.MapDelete("/{fileName}", DeleteFile)
             .AllowAnonymous()
             .DisableAntiforgery();
    }

    private static async Task<IResult> UploadFile(
        IFormFile file,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return Results.BadRequest(new { error = "هیچ فایلی انتخاب نشده است" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml" };
        if (!allowedTypes.Contains(file.ContentType))
            return Results.BadRequest(new { error = "نوع فایل مجاز نیست. فقط تصاویر مجاز هستند." });

        if (file.Length > 5 * 1024 * 1024)
            return Results.BadRequest(new { error = "حجم فایل نباید بیشتر از 5 مگابایت باشد" });

        try
        {
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadPath = Path.Combine(env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var fileUrl = $"/uploads/{fileName}";

            return Results.Ok(new
            {
                url = fileUrl,
                fileName = fileName,
                size = file.Length,
                contentType = file.ContentType,
                success = true
            });
        }
        catch (Exception ex)
        {
            return Results.StatusCode(500);
        }
    }

    private static async Task<IResult> DeleteFile(
        string fileName,
        IWebHostEnvironment env)
    {
        var filePath = Path.Combine(env.WebRootPath, "uploads", fileName);

        if (!File.Exists(filePath))
            return Results.NotFound(new { error = "فایل یافت نشد" });

        File.Delete(filePath);

        return Results.Ok(new { message = "فایل با موفقیت حذف شد" });
    }
}