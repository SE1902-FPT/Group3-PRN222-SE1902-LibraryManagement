using Group3_SE1902_PRN222_LibraryManagement.Models;
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

        public List<Book> Books { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int? SelectedCategoryId { get; set; }
        public int TotalResults { get; set; }

        public async Task OnGetAsync(string? search, int? categoryId)
        {
            SearchTerm = search?.Trim() ?? string.Empty;
            SelectedCategoryId = categoryId;

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
                    (b.Publisher != null && b.Publisher.Contains(SearchTerm)) ||
                    (b.Isbn != null && b.Isbn.Contains(SearchTerm)));
            }

            TotalResults = await query.CountAsync();

            Books = await query
                .OrderByDescending(b => b.BookId)
                .ToListAsync();
        }
    }
}
