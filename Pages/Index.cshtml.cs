using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ThuVienContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger, ThuVienContext context)
        {
            _logger = logger;
            _context = context;
        }

        public User? CurrentUser { get; set; }
        public List<Notification> Notifications { get; set; } = new();
        public List<TeacherRecommendation> Recommendations { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Book> FeaturedBooks { get; set; } = new();
        public string? UserClassName { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public int? SelectedCategoryId { get; set; }
        public int TotalMatchedBooks { get; set; }
        public bool HasActiveFilters => !string.IsNullOrWhiteSpace(SearchTerm) || SelectedCategoryId.HasValue;

        public async Task OnGetAsync(string? search, int? categoryId)
        {
            SearchTerm = search?.Trim() ?? string.Empty;
            SelectedCategoryId = categoryId;

            var email = User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                CurrentUser = await _context.Users
                    .Include(u => u.ClassesNavigation)
                    .Include(u => u.BorrowRecordStudents)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }

            if (CurrentUser != null)
            {
                var userClass = CurrentUser.ClassesNavigation.FirstOrDefault();
                UserClassName = userClass?.ClassName ?? "N/A";

                Notifications = await _context.Notifications
                    .Where(n => n.UserId == CurrentUser.UserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                Recommendations = await _context.TeacherRecommendations
                    .Include(r => r.Book)
                        .ThenInclude(b => b!.BookCopies)
                    .Include(r => r.Teacher)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(2)
                    .ToListAsync();
            }
            else
            {
                Recommendations = await _context.TeacherRecommendations
                    .Include(r => r.Book)
                        .ThenInclude(b => b!.BookCopies)
                    .Include(r => r.Teacher)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(2)
                    .ToListAsync();
            }

            Categories = await _context.Categories.ToListAsync();

            var booksQuery = _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                .AsQueryable();

            if (SelectedCategoryId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == SelectedCategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(SearchTerm) ||
                    (b.Author != null && b.Author.Contains(SearchTerm)) ||
                    (b.Publisher != null && b.Publisher.Contains(SearchTerm)) ||
                    (b.Isbn != null && b.Isbn.Contains(SearchTerm)));
            }

            TotalMatchedBooks = await booksQuery.CountAsync();

            FeaturedBooks = await booksQuery
                .OrderByDescending(b => b.BookId)
                .Take(8)
                .ToListAsync();
        }
    }
}
