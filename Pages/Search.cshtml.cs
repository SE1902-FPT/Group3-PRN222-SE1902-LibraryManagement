using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages
{
    public class SearchModel : PageModel
    {
        private readonly ThuVienContext _context;

        public SearchModel(ThuVienContext context)
        {
            _context = context;
        }

        public User? CurrentUser { get; set; }
        public string? UserClassName { get; set; }
        public List<Category> Categories { get; set; } = new();
        public List<Book> Books { get; set; } = new();
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
            UserClassName = CurrentUser?.ClassesNavigation.FirstOrDefault()?.ClassName ?? "N/A";

            if (CurrentUser != null)
            {
                FavoriteBookIds = await _context.FavoriteBooks
                    .Where(fb => fb.StudentId == CurrentUser.UserId)
                    .Select(fb => fb.BookId)
                    .ToListAsync();

                FavoriteCount = FavoriteBookIds.Count;
            }

            Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var query = _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                .AsQueryable();

            if (SelectedCategoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == SelectedCategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(b =>
                    b.Title.Contains(SearchTerm) ||
                    (b.Author != null && b.Author.Contains(SearchTerm)) ||
                    (b.Publisher != null && b.Publisher.Contains(SearchTerm)));
            }

            if (Availability == "available")
            {
                query = query.Where(b => b.BookCopies.Any(c => c.Status == "Available"));
            }

            Books = await query
                .OrderByDescending(b => b.BookId)
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
                        .FirstOrDefaultAsync(u => u.Email == email);
                }
            }

            return await _context.Users
                .Include(u => u.ClassesNavigation)
                .FirstOrDefaultAsync(u => u.RoleId == 1);
        }
    }
}
