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
    public class DetailsModel : PageModel
    {
        private readonly ThuVienContext _context;

        public DetailsModel(ThuVienContext context)
        {
            _context = context;
        }

        public User User { get; set; } = default!;

        // Danh sách liên kết học sinh/phụ huynh
        public List<ParentStudent> LinkedUsers { get; set; } = new List<ParentStudent>();

        // Thông tin lớp học (Nếu là giáo viên chủ nhiệm hoặc sinh viên trong lớp)
        public List<Class> ManagedClasses { get; set; } = new List<Class>(); // Cho Teacher
        public List<Class> JoinedClasses { get; set; } = new List<Class>();  // Cho Student

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Load User cùng với Role và thông tin Lớp học
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Classes)           // Lớp mà User này làm Teacher
                .Include(u => u.ClassesNavigation) // Lớp mà User này là Student
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null) return NotFound();
            User = user;

            // 1. Xử lý thông tin Phụ huynh/Học sinh
            if (User.Role?.RoleName == "Student")
            {
                LinkedUsers = await _context.ParentStudents
                    .Include(ps => ps.Parent)
                    .Where(ps => ps.StudentId == id).ToListAsync();

                JoinedClasses = User.ClassesNavigation.ToList();
            }
            else if (User.Role?.RoleName == "Parent")
            {
                LinkedUsers = await _context.ParentStudents
                    .Include(ps => ps.Student)
                    .Where(ps => ps.ParentId == id).ToListAsync();
            }
            else if (User.Role?.RoleName == "Teacher")
            {
                ManagedClasses = User.Classes.ToList();
            }

            return Page();
        }
    }
}