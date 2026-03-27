using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.BookManagement
{
    public class EditModel : PageModel
    {
        private readonly ThuVienContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(ThuVienContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Book Book { get; set; } = default!;

        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }
            Book = book;

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", book.CategoryId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                return Page();
            }

            var existingBook = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.BookId == Book.BookId);
            if (existingBook == null)
            {
                return NotFound();
            }

            var savedUploadUrl = await TrySaveUploadAsync();
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", Book.CategoryId);
                return Page();
            }

            Book.ImageUrl = !string.IsNullOrWhiteSpace(savedUploadUrl) ? savedUploadUrl : existingBook.ImageUrl;

            _context.Attach(Book).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(Book.BookId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookId == id);
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