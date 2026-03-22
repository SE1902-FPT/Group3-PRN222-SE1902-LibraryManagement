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

        public async Task OnGetAsync()
        {
            // For demo purposes, we fetch the first student if no session exists
            // In a real app, you would get this from the logged-in user's identity
            CurrentUser = await _context.Users
                .Include(u => u.ClassesNavigation)
                .FirstOrDefaultAsync(u => u.RoleId == 3); // Assuming 3 is Student role

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
        }
    }
}
