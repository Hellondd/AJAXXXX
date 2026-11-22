using Microsoft.AspNetCore.Mvc;

namespace Mvcpr.Controllers;

public class FilesController(IWebHostEnvironment environment) : Controller
{
    private const long MaxSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".pdf", ".png", ".jpg", ".jpeg" };
    private string StoragePath => Path.Combine(environment.ContentRootPath, "Storage");

    public IActionResult Index()
    {
        Directory.CreateDirectory(StoragePath);
        return View(Directory.GetFiles(StoragePath).Select(Path.GetFileName)
            .Where(name => name is not null).OrderBy(name => name).ToArray());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxSize + 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0 || file.Length > MaxSize ||
            !AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
        {
            TempData["Message"] = "Выберите TXT, PDF, PNG или JPG размером до 10 МБ.";
            return RedirectToAction(nameof(Index));
        }
        Directory.CreateDirectory(StoragePath);
        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using var stream = new FileStream(Path.Combine(StoragePath, safeName), FileMode.CreateNew);
        await file.CopyToAsync(stream);
        TempData["Message"] = $"Файл загружен: {safeName}";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Download(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name)) return BadRequest();
        var path = Path.Combine(StoragePath, name);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, "application/octet-stream", name);
    }
}
