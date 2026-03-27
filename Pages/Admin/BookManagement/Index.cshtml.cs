using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.BookManagement
{
    public class IndexModel : PageModel
    {
        private readonly ThuVienContext _context;

        public IndexModel(ThuVienContext context)
        {
            _context = context;
        }

        public IList<Book> Book { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        public SelectList Categories { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");

            var booksQuery = _context.Books
                .Include(b => b.Category)
                .AsQueryable();

            //seach
            if (!string.IsNullOrEmpty(SearchString))
            {
                booksQuery = booksQuery.Where(s => s.Title.Contains(SearchString)
                                               || s.Author.Contains(SearchString)
                                               || s.Isbn.Contains(SearchString));
            }

            //filter
            if (CategoryFilter.HasValue)
            {
                booksQuery = booksQuery.Where(x => x.CategoryId == CategoryFilter);
            }

            Book = await booksQuery.ToListAsync();
        }
    }
}