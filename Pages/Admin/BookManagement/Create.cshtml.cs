using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.BookManagement
{
    public class CreateModel : PageModel
    {
        private readonly ThuVienContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(ThuVienContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult OnGet()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return Page();
        }

        [BindProperty]
        public Book Book { get; set; } = default!;

        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                return Page();
            }

            var savedUploadUrl = await TrySaveUploadAsync();
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(savedUploadUrl))
            {
                Book.ImageUrl = savedUploadUrl;
            }

            _context.Books.Add(Book);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task<string?> TrySaveUploadAsync()
        {
            if (UploadImage == null || UploadImage.Length <= 0)
            {
                return null;
            }

            const long maxBytes = 5 * 1024 * 1024;
            if (UploadImage.Length > maxBytes)
            {
                ModelState.AddModelError(string.Empty, "Ảnh quá lớn (tối đa 5MB).");
                return null;
            }

            var ext = Path.GetExtension(UploadImage.FileName).ToLowerInvariant();
            if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".gif" and not ".webp")
            {
                ModelState.AddModelError(string.Empty, "Định dạng ảnh không hỗ trợ. Chỉ nhận: png, jpg, jpeg, gif, webp.");
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
    }
}