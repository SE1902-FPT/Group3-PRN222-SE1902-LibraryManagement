using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.CategoryManagement
{
    public class IndexModel : PageModel
    {
        private readonly Group3_SE1902_PRN222_LibraryManagement.Models.ThuVienContext _context;

        public IndexModel(Group3_SE1902_PRN222_LibraryManagement.Models.ThuVienContext context)
        {
            _context = context;
        }

        public IList<Category> Category { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Category = await _context.Categories.ToListAsync();
        }
    }
}
