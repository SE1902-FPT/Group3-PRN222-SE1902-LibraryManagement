using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Books
{
    public class DetailsModel : PageModel
    {
        private readonly ThuVienContext _context;

        public DetailsModel(ThuVienContext context)
        {
            _context = context;
        }

        public Book Book { get; set; } = null!;
        public List<TeacherRecommendation> TeacherRecommendations { get; set; } = new();
        public int AvailableCopies { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            Book = book;
            AvailableCopies = Book.BookCopies.Count(c => c.Status == "Available");

            TeacherRecommendations = await _context.TeacherRecommendations
                .Include(r => r.Teacher)
                .Where(r => r.BookId == id)
                .ToListAsync();

            return Page();
        }
    }
}
