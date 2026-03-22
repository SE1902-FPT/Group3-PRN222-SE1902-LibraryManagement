using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.TeacherRecommendations
{
    public class IndexModel : PageModel
    {
        private readonly ThuVienContext _context;

        public IndexModel(ThuVienContext context)
        {
            _context = context;
        }

        public List<TeacherRecommendation> TeacherRecommendations { get; set; } = new();

        public async Task OnGetAsync()
        {
            TeacherRecommendations = await _context.TeacherRecommendations
                .Include(r => r.Book)
                    .ThenInclude(b => b!.Category)
                .Include(r => r.Book)
                    .ThenInclude(b => b!.BookCopies)
                .Include(r => r.Teacher)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
