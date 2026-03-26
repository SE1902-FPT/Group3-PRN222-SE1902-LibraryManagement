using System.ComponentModel.DataAnnotations;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

[Authorize(Roles = "Librarian")]
public class AddBookModel : PageModel
{
    private readonly ThuVienContext _context;
    private readonly IWebHostEnvironment _env;

    public AddBookModel(ThuVienContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public List<Category> Categories { get; set; } = new();
    public List<string> AssetImages { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public AddBookInput Input { get; set; } = new();

    [BindProperty]
    public IFormFile? UploadImage { get; set; }

    public class AddBookInput
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Author { get; set; }

        [StringLength(20)]
        public string? Isbn { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(100)]
        public string? Publisher { get; set; }

        public int? PublishYear { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(1, 9999)]
        public int CopiesToAdd { get; set; } = 1;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Dữ liệu nhập chưa hợp lệ. Vui lòng kiểm tra lại.";
            return Page();
        }

        var savedUploadUrl = await TrySaveUploadAsync();
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            return Page();
        }

        var resolvedImageUrl = ResolveImageUrl(Input.ImageUrl, savedUploadUrl);

        var book = new Book
        {
            Title = Input.Title.Trim(),
            Author = string.IsNullOrWhiteSpace(Input.Author) ? null : Input.Author.Trim(),
            Isbn = string.IsNullOrWhiteSpace(Input.Isbn) ? null : Input.Isbn.Trim(),
            CategoryId = Input.CategoryId,
            Publisher = string.IsNullOrWhiteSpace(Input.Publisher) ? null : Input.Publisher.Trim(),
            PublishYear = Input.PublishYear,
            ImageUrl = resolvedImageUrl,
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim()
        };

        for (var i = 0; i < Input.CopiesToAdd; i++)
        {
            book.BookCopies.Add(new BookCopy
            {
                Status = "Available"
            });
        }

        book.TotalQuantity = book.BookCopies.Count;

        _context.Books.Add(book);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            ErrorMessage = ex.InnerException?.Message.Contains("UQ__Books__447D36EA", StringComparison.OrdinalIgnoreCase) == true
                ? "ISBN đã tồn tại. Vui lòng nhập ISBN khác (hoặc để trống)."
                : $"Không thể lưu dữ liệu sách: {ex.InnerException?.Message ?? ex.Message}";
            return Page();
        }

        SuccessMessage = $"Đã tạo sách \"{book.Title}\" và thêm {Input.CopiesToAdd} bản sách.";
        Input = new AddBookInput { CopiesToAdd = 1 };

        // Quay lại trang dashboard Librarian
        return RedirectToPage("/Librarian/Libralian_Dashboard");
    }

    private async Task<string?> TrySaveUploadAsync()
    {
        if (UploadImage == null || UploadImage.Length <= 0)
        {
            return null;
        }

        // Basic validation
        const long maxBytes = 5 * 1024 * 1024; // 5MB
        if (UploadImage.Length > maxBytes)
        {
            ErrorMessage = "Ảnh quá lớn (tối đa 5MB).";
            return null;
        }

        var ext = Path.GetExtension(UploadImage.FileName).ToLowerInvariant();
        if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".gif" and not ".webp")
        {
            ErrorMessage = "Định dạng ảnh không hỗ trợ. Chỉ nhận: png, jpg, jpeg, gif, webp.";
            return null;
        }

        var assetDir = Path.Combine(_env.WebRootPath, "Asset");
        Directory.CreateDirectory(assetDir);

        var safeBaseName = Path.GetFileNameWithoutExtension(UploadImage.FileName);
        safeBaseName = string.Concat(safeBaseName.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'));
        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = "book";
        }

        var fileName = $"{safeBaseName}-{Guid.NewGuid():N}{ext}";
        var absPath = Path.Combine(assetDir, fileName);

        await using (var stream = System.IO.File.Create(absPath))
        {
            await UploadImage.CopyToAsync(stream);
        }

        return "/Asset/" + fileName;
    }

    private static string? ResolveImageUrl(string? rawImageUrl, string? uploadedAssetUrl)
    {
        if (!string.IsNullOrWhiteSpace(uploadedAssetUrl))
        {
            return uploadedAssetUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(rawImageUrl))
        {
            return null;
        }

        var trimmed = rawImageUrl.Trim();

        // If user typed an absolute Windows path, it won't work in browser
        if (trimmed.Contains(":\\", StringComparison.Ordinal))
        {
            return null;
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // Normalize relative paths to web-root relative
        return trimmed.StartsWith("/") ? trimmed : "/" + trimmed;
    }
}

