using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

[Authorize(Roles = "Librarian")]
public class Librarian_DashboardModel : PageModel
{
    private readonly ThuVienContext _context;

    public Librarian_DashboardModel(ThuVienContext context)
    {
        _context = context;
    }

    public int? LibrarianId { get; set; }
    public string? LibrarianName { get; set; }
    public string? ErrorMessage { get; set; }

    public List<BookRow> Books { get; set; } = new();

    public class BookRow
    {
        public int BookId { get; set; }
        public string? ImageUrl { get; set; }
        public string Title { get; set; } = "-";
        public string? Author { get; set; }
        public string? CategoryName { get; set; }
        public int AvailableCount { get; set; }
        public int BorrowedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? librarianId)
    {
        const string statusAvailable = "Available";
        const string statusBorrowed = "Borrowed";

        var currentUser = await GetCurrentLibrarianAsync();
        if (currentUser == null)
        {
            return RedirectToPage("/Login", new { error = "access_denied" });
        }

        LibrarianId = currentUser.UserId;
        LibrarianName = currentUser.FullName;

        // Dashboard hiển thị nhanh tình trạng sách (top newest)
        Books = await _context.Books
            .Include(b => b.Category)
            .Include(b => b.BookCopies)
            .OrderByDescending(b => b.BookId)
            .Take(10)
            .Select(b => new BookRow
            {
                BookId = b.BookId,
                ImageUrl = b.ImageUrl,
                Title = b.Title,
                Author = b.Author,
                CategoryName = b.Category != null ? b.Category.CategoryName : null,
                AvailableCount = b.BookCopies.Count(c => c.Status == statusAvailable),
                BorrowedCount = b.BookCopies.Count(c => c.Status == statusBorrowed),
                TotalCount = b.BookCopies.Count
            })
            .ToListAsync();

        return Page();
    }

    private async Task<User?> GetCurrentLibrarianAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.RoleId == 4);
    }
}

