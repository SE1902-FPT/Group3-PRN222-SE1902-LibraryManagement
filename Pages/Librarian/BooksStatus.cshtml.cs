using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

public class BooksStatusModel : PageModel
{
    private readonly ThuVienContext _context;

    public BooksStatusModel(ThuVienContext context)
    {
        _context = context;
    }

    public string? ErrorMessage { get; set; }
    public string Query { get; set; } = string.Empty;

    public List<BookStatusRow> BookRows { get; set; } = new();

    public class BookStatusRow
    {
        public string Title { get; set; } = "-";
        public string? CategoryName { get; set; }
        public string? Author { get; set; }
        public int AvailableCount { get; set; }
        public int BorrowedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public async Task OnGetAsync(string? q)
    {
        try
        {
            Query = q?.Trim() ?? string.Empty;

            var booksQuery = _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Query))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(Query) ||
                    (b.Author != null && b.Author.Contains(Query)));
            }

            BookRows = await booksQuery
                .OrderByDescending(b => b.BookId)
                .Select(b => new BookStatusRow
                {
                    Title = b.Title,
                    CategoryName = b.Category != null ? b.Category.CategoryName : null,
                    Author = b.Author,
                    AvailableCount = b.BookCopies.Count(c => c.Status == "Available"),
                    BorrowedCount = b.BookCopies.Count(c => c.Status == "Borrowed"),
                    TotalCount = b.BookCopies.Count
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi lấy dữ liệu: {ex.Message}";
        }
    }
}

