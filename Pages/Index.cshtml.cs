using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;

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
        public List<int> FavoriteBookIds { get; set; } = new();
        public int FavoriteCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public int? SelectedCategoryId { get; set; }
        public string Availability { get; set; } = "all";

        public async Task OnGetAsync(int? categoryId, string? search, string? availability)
        {
            SearchTerm = search?.Trim() ?? string.Empty;
            SelectedCategoryId = categoryId;
            Availability = string.Equals(availability, "available", StringComparison.OrdinalIgnoreCase)
                ? "available"
                : "all";

            CurrentUser = await LoadCurrentStudentAsync();

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

                FavoriteBookIds = await _context.FavoriteBooks
                    .Where(fb => fb.StudentId == CurrentUser.UserId)
                    .Select(fb => fb.BookId)
                    .ToListAsync();

                FavoriteCount = FavoriteBookIds.Count;
            }

            Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

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
                    (b.Publisher != null && b.Publisher.Contains(SearchTerm)));
            }

            if (Availability == "available")
            {
                booksQuery = booksQuery.Where(b => b.BookCopies.Any(c => c.Status == "Available"));
            }

            FeaturedBooks = await booksQuery
                .OrderByDescending(b => b.BookId)
                .Take(8)
                .ToListAsync();
        }

        private async Task<User?> LoadCurrentStudentAsync()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Student"))
            {
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    return await _context.Users
                        .Include(u => u.ClassesNavigation)
                        .Include(u => u.BorrowRecordStudents)
                        .FirstOrDefaultAsync(u => u.Email == email);
                }
            }

            return await _context.Users
                .Include(u => u.ClassesNavigation)
                .Include(u => u.BorrowRecordStudents)
                .FirstOrDefaultAsync(u => u.RoleId == 1);
        }
    }
}
