using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Teacher
{
    public class IndexModel : PageModel
    {
        private readonly ThuVienContext _context;

        public IndexModel(ThuVienContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string RecommendationNote { get; set; } = string.Empty;

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? MessageType { get; set; }

        public User? CurrentTeacher { get; set; }
        public List<Class> TeachingClasses { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Book> Books { get; set; } = new();
        public Dictionary<int, TeacherRecommendation> TeacherRecommendationMap { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int? SelectedCategoryId { get; set; }

        public async Task<IActionResult> OnGetAsync(string? search, int? categoryId)
        {
            var teacher = await LoadCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToPage("/Login", new { error = "access_denied" });
            }

            CurrentTeacher = teacher;
            SearchTerm = search?.Trim() ?? string.Empty;
            SelectedCategoryId = categoryId;

            TeachingClasses = await _context.Classes
                .Where(c => c.TeacherId == teacher.UserId)
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            TeacherRecommendationMap = await _context.TeacherRecommendations
                .Where(r => r.TeacherId == teacher.UserId && r.BookId != null)
                .ToDictionaryAsync(r => r.BookId!.Value, r => r);

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
                    (b.Publisher != null && b.Publisher.Contains(SearchTerm)) ||
                    (b.Isbn != null && b.Isbn.Contains(SearchTerm)));
            }

            Books = await query
                .OrderByDescending(b => b.BookId)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRecommendAsync(int bookId, string? search, int? categoryId)
        {
            var teacher = await LoadCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToPage("/Login", new { error = "access_denied" });
            }

            var existing = await _context.TeacherRecommendations
                .FirstOrDefaultAsync(r => r.TeacherId == teacher.UserId && r.BookId == bookId);

            if (existing == null)
            {
                _context.TeacherRecommendations.Add(new TeacherRecommendation
                {
                    TeacherId = teacher.UserId,
                    BookId = bookId,
                    Note = string.IsNullOrWhiteSpace(RecommendationNote) ? null : RecommendationNote.Trim(),
                    CreatedAt = DateTime.Now
                });

                Message = "Đã thêm đề xuất sách cho học sinh.";
            }
            else
            {
                existing.Note = string.IsNullOrWhiteSpace(RecommendationNote) ? existing.Note : RecommendationNote.Trim();
                existing.CreatedAt = DateTime.Now;
                Message = "Đã cập nhật ghi chú đề xuất.";
            }

            MessageType = "success";
            await _context.SaveChangesAsync();
            return RedirectToPage(new { search, categoryId });
        }

        public async Task<IActionResult> OnPostRemoveRecommendationAsync(int recommendationId, string? search, int? categoryId)
        {
            var teacher = await LoadCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToPage("/Login", new { error = "access_denied" });
            }

            var recommendation = await _context.TeacherRecommendations
                .FirstOrDefaultAsync(r => r.RecommendationId == recommendationId && r.TeacherId == teacher.UserId);

            if (recommendation != null)
            {
                _context.TeacherRecommendations.Remove(recommendation);
                await _context.SaveChangesAsync();
                Message = "Đã gỡ đề xuất sách.";
                MessageType = "success";
            }

            return RedirectToPage(new { search, categoryId });
        }

        private async Task<User?> LoadCurrentTeacherAsync()
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Teacher"))
            {
                return null;
            }

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _context.Users
                .Include(u => u.Classes)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
