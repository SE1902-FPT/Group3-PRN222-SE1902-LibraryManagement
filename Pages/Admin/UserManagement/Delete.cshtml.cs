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
    public class DeleteModel : PageModel
    {
        private readonly ThuVienContext _context;

        public DeleteModel(ThuVienContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null) return NotFound();
            else User = user;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.ClassesNavigation)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user != null)
            {
                try
                {
                    // Saturday Learn(xóa quan hệ lớp)
                    user.ClassesNavigation.Clear();

                    // Matcha mát mẻ (Xóa quan hệ phụ huynh)
                    var parentLinks = _context.ParentStudents
                        .Where(ps => ps.ParentId == id || ps.StudentId == id);
                    _context.ParentStudents.RemoveRange(parentLinks);

                    //Gỡ giáo viên
                    var managedClasses = await _context.Classes
                        .Where(c => c.TeacherId == id).ToListAsync();
                    foreach (var cls in managedClasses) cls.TeacherId = null;

                    // Kiểm tra xem User này có đang mượn sách hoặc có yêu cầu mượn nào không
                    bool hasHistory = await _context.BorrowRecords.AnyAsync(br => br.StudentId == id || br.ProcessedBy == id)
                                   || await _context.BorrowRequests.AnyAsync(rq => rq.StudentId == id || rq.ApprovedBy == id);

                    if (hasHistory)
                    {
                        user.Status = "Inactive";
                        _context.Users.Update(user);
                    }
                    else
                    {
                        //Không mượn thì cook
                        _context.Users.Remove(user);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Không thể xóa: " + (ex.InnerException?.Message ?? ex.Message);
                    return RedirectToPage("./Index");
                }
            }

            return RedirectToPage("./Index");
        }
    }
}