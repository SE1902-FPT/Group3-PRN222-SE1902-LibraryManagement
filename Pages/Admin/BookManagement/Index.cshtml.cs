using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần thiết để làm Dropdown Filter
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

        // Lưu trữ giá trị search/filter để hiển thị lại trên Form sau khi load trang
        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        public SelectList Categories { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // 1. Nạp danh sách thể loại cho Dropdown Filter
            Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");

            // 2. Khởi tạo truy vấn gốc
            var booksQuery = _context.Books
                .Include(b => b.Category)
                .AsQueryable();

            // 3. Lọc theo từ khóa (Search) nếu có
            if (!string.IsNullOrEmpty(SearchString))
            {
                booksQuery = booksQuery.Where(s => s.Title.Contains(SearchString)
                                               || s.Author.Contains(SearchString)
                                               || s.Isbn.Contains(SearchString));
            }

            // 4. Lọc theo Thể loại (Filter) nếu có
            if (CategoryFilter.HasValue)
            {
                booksQuery = booksQuery.Where(x => x.CategoryId == CategoryFilter);
            }

            Book = await booksQuery.ToListAsync();
        }
    }
}