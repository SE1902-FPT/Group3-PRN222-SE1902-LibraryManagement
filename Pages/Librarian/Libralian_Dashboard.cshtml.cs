using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

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
        public string Title { get; set; } = "-";
        public string? Author { get; set; }
        public string? CategoryName { get; set; }
        public int AvailableCount { get; set; }
        public int BorrowedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public async Task OnGetAsync(int? librarianId)
    {
        const int librarianRoleId = 4;
        const string statusAvailable = "Available";
        const string statusBorrowed = "Borrowed";

        var resolved = false;

        if (librarianId.HasValue)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserId == librarianId.Value && u.RoleId == librarianRoleId);

            if (user == null)
            {
                ErrorMessage = "librarianId không hợp lệ (không phải Librarian).";
                LibrarianId = null;
                LibrarianName = null;
            }
            else
            {
                LibrarianId = user.UserId;
                LibrarianName = user.FullName;
                resolved = true;
            }
        }
        else
        {
            var firstLibrarian = await _context.Users
                .FirstOrDefaultAsync(u => u.RoleId == librarianRoleId);

            if (firstLibrarian == null)
            {
                ErrorMessage = "Chưa có tài khoản Librarian trong database (RoleId = 4).";
                LibrarianId = null;
                LibrarianName = null;
            }
            else
            {
                LibrarianId = firstLibrarian.UserId;
                LibrarianName = firstLibrarian.FullName;
                resolved = true;
            }
        }

        if (!resolved)
        {
            return;
        }

        // Dashboard hiển thị nhanh tình trạng sách (top newest)
        Books = await _context.Books
            .Include(b => b.Category)
            .Include(b => b.BookCopies)
            .OrderByDescending(b => b.BookId)
            .Take(10)
            .Select(b => new BookRow
            {
                BookId = b.BookId,
                Title = b.Title,
                Author = b.Author,
                CategoryName = b.Category != null ? b.Category.CategoryName : null,
                AvailableCount = b.BookCopies.Count(c => c.Status == statusAvailable),
                BorrowedCount = b.BookCopies.Count(c => c.Status == statusBorrowed),
                TotalCount = b.BookCopies.Count
            })
            .ToListAsync();
    }
}

