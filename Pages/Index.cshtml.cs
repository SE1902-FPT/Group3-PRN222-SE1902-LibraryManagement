using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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

        public bool IsAuthenticated { get; set; }
        public string? LoggedInUserName { get; set; }
        public string? LoggedInUserRole { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (IsAuthenticated)
            {
                LoggedInUserName = User.FindFirst(ClaimTypes.Name)?.Value;
                LoggedInUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Try to find the authenticated user in the database
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(email))
                {
                    CurrentUser = await _context.Users
                        .Include(u => u.ClassesNavigation)
                        .FirstOrDefaultAsync(u => u.Email == email);
                }
            }

            // Fallback: fetch first student for demo purposes if no authenticated user found
            if (CurrentUser == null)
            {
                CurrentUser = await _context.Users
                    .Include(u => u.ClassesNavigation)
                    .FirstOrDefaultAsync(u => u.RoleId == 3);
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

            Categories = await _context.Categories.ToListAsync();

            FeaturedBooks = await _context.Books
                .Include(b => b.Category)
                .OrderByDescending(b => b.BookId)
                .Take(5)
                .ToListAsync();

            return Page();
        }
    }
}
