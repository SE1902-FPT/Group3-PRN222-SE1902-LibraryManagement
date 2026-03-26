using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.UserManagement
{
    public class IndexModel : PageModel
    {
        private readonly ThuVienContext _context;

        public IndexModel(ThuVienContext context)
        {
            _context = context;
        }

        public IList<User> Users { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RoleFilter { get; set; }

        public SelectList RoleList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // 1. Nạp danh sách Role để filter
            RoleList = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "RoleName");

            // 2. Query cơ bản kèm Include Role
            var userQuery = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            // 3. Search theo FullName hoặc Email
            if (!string.IsNullOrEmpty(SearchString))
            {
                userQuery = userQuery.Where(u => u.FullName.Contains(SearchString)
                                              || u.Email.Contains(SearchString));
            }

            // 4. Filter theo RoleId
            if (RoleFilter.HasValue)
            {
                userQuery = userQuery.Where(u => u.RoleId == RoleFilter.Value);
            }

            Users = await userQuery.ToListAsync();
        }
    }
}