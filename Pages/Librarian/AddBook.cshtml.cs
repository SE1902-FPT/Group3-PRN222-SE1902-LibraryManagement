using System.ComponentModel.DataAnnotations;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

public class AddBookModel : PageModel
{
    private readonly ThuVienContext _context;

    public AddBookModel(ThuVienContext context)
    {
        _context = context;
    }

    public List<Category> Categories { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public AddBookInput Input { get; set; } = new();

    public class AddBookInput
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Author { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(100)]
        public string? Publisher { get; set; }

        public int? PublishYear { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(1, 9999)]
        public int CopiesToAdd { get; set; } = 1;
    }

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Dữ liệu nhập chưa hợp lệ. Vui lòng kiểm tra lại.";
            return Page();
        }

        var book = new Book
        {
            Title = Input.Title.Trim(),
            Author = string.IsNullOrWhiteSpace(Input.Author) ? null : Input.Author.Trim(),
            CategoryId = Input.CategoryId,
            Publisher = string.IsNullOrWhiteSpace(Input.Publisher) ? null : Input.Publisher.Trim(),
            PublishYear = Input.PublishYear,
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim()
        };

        for (var i = 0; i < Input.CopiesToAdd; i++)
        {
            book.BookCopies.Add(new BookCopy
            {
                Status = "Available"
            });
        }

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        SuccessMessage = $"Đã tạo sách \"{book.Title}\" và thêm {Input.CopiesToAdd} bản sách.";
        Input = new AddBookInput { CopiesToAdd = 1 };

        // Quay lại trang dashboard Librarian
        return RedirectToPage("/Librarian/Libralian_Dashboard");
    }
}

