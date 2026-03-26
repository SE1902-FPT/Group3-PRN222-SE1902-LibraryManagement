using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.UserManagement
{
    public class IndexModel : PageModel
    {
        private readonly Group3_SE1902_PRN222_LibraryManagement.Models.ThuVienContext _context;

        public IndexModel(Group3_SE1902_PRN222_LibraryManagement.Models.ThuVienContext context)
        {
            _context = context;
        }

        public IList<User> Users { get; set; } = new List<User>();

        public async Task OnGetAsync()
        {
            Users = await _context.Users
                .Include(u => u.Role).ToListAsync();
        }
    }
}
