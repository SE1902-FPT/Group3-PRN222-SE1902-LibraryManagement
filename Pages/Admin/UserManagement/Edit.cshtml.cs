using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using BCrypt.Net;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.UserManagement
{
    public class EditModel : PageModel
    {
        private readonly ThuVienContext _context;

        public EditModel(ThuVienContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User User { get; set; } = default!;

        [BindProperty]
        public string? NewPassword { get; set; }

        [BindProperty]
        public int? SelectedClassId { get; set; }

        [BindProperty]
        public int? SelectedStudentId { get; set; }

        [BindProperty]
        public string? Relationship { get; set; }

        [BindProperty]
        public string? ParentPhone { get; set; }

        [BindProperty]
        public string? ParentAddress { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Load dữ liệu với AsNoTracking để tránh xung đột khi Save sau này
            User = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Classes)
                .Include(u => u.ClassesNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (User == null) return NotFound();

            // Nạp thông tin phụ huynh nếu có
            var link = await _context.ParentStudents.AsNoTracking().FirstOrDefaultAsync(ps => ps.ParentId == id);
            if (link != null)
            {
                SelectedStudentId = link.StudentId;
                Relationship = link.Relationship;
                ParentPhone = link.Phone;
                ParentAddress = link.Address;
            }

            // Nạp ID lớp hiện tại
            if (User.Role?.RoleName == "Teacher")
                SelectedClassId = User.Classes.FirstOrDefault()?.ClassId;
            else if (User.Role?.RoleName == "Student")
                SelectedClassId = User.ClassesNavigation.FirstOrDefault()?.ClassId;

            await PopulateLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // BƯỚC 1: Xóa TOÀN BỘ lỗi Validation liên quan đến các bảng phụ
            // Đây là nguyên nhân chính khiến ModelState.IsValid luôn bằng False
            var relatedProperties = new[] { "User.Role", "User.Classes", "User.ClassesNavigation",
                                     "User.ParentStudentParents", "User.ParentStudentStudents",
                                     "User.Borrowings", "User.Reservations" };

            foreach (var property in relatedProperties)
            {
                ModelState.Remove(property);
            }

            // BƯỚC 2: Kiểm tra lại. Nếu vẫn lỗi, ta in ra Console để Debug
            if (!ModelState.IsValid)
            {
                // Đoạn code này sẽ giúp bạn thấy lỗi ở cửa sổ Output của Visual Studio
                var errors = ModelState.SelectMany(x => x.Value.Errors.Select(p => p.ErrorMessage));
                foreach (var err in errors) Console.WriteLine("LOG_LỖI_VALIDATION: " + err);

                await PopulateLists();
                return Page();
            }

            // BƯỚC 3: Tìm User thực trong Database (Dùng Include để cập nhật bảng trung gian)
            var userToUpdate = await _context.Users
                .Include(u => u.Classes)
                .Include(u => u.ClassesNavigation)
                .FirstOrDefaultAsync(u => u.UserId == User.UserId);

            if (userToUpdate == null) return NotFound();

            try
            {
                // Cập nhật thông tin cơ bản
                userToUpdate.FullName = User.FullName;
                userToUpdate.Email = User.Email;
                userToUpdate.RoleId = User.RoleId;
                userToUpdate.Status = User.Status;

                if (!string.IsNullOrEmpty(NewPassword))
                {
                    userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                }

                // BƯỚC 4: Xử lý logic theo Role (Xóa cũ - Thêm mới)
                var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == User.RoleId);

                // Xóa sạch các liên kết cũ trước để tránh xung đột
                userToUpdate.ClassesNavigation.Clear(); // Xóa quan hệ StudentClass
                var oldClasses = _context.Classes.Where(c => c.TeacherId == userToUpdate.UserId);
                foreach (var c in oldClasses) c.TeacherId = null; // Gỡ Teacher

                var oldParentLinks = _context.ParentStudents.Where(ps => ps.ParentId == userToUpdate.UserId);
                _context.ParentStudents.RemoveRange(oldParentLinks); // Xóa Parent link

                // Thêm liên kết mới dựa trên Role hiện tại
                if (role?.RoleName == "Student" && SelectedClassId.HasValue)
                {
                    var targetClass = await _context.Classes.FindAsync(SelectedClassId);
                    if (targetClass != null) userToUpdate.ClassesNavigation.Add(targetClass);
                }
                else if (role?.RoleName == "Teacher" && SelectedClassId.HasValue)
                {
                    var targetClass = await _context.Classes.FindAsync(SelectedClassId);
                    if (targetClass != null) targetClass.TeacherId = userToUpdate.UserId;
                }
                else if (role?.RoleName == "Parent" && SelectedStudentId.HasValue)
                {
                    _context.ParentStudents.Add(new ParentStudent
                    {
                        ParentId = userToUpdate.UserId,
                        StudentId = SelectedStudentId.Value,
                        Relationship = Relationship,
                        Phone = ParentPhone,
                        Address = ParentAddress
                    });
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi DB: " + (ex.InnerException?.Message ?? ex.Message));
                await PopulateLists();
                return Page();
            }
        }

        private async Task PopulateLists()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", User?.RoleId);
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", SelectedClassId);

            var students = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Student")
                .Select(u => new { u.UserId, DisplayName = u.FullName + " (" + u.Email + ")" })
                .AsNoTracking()
                .ToListAsync();
            ViewData["StudentId"] = new SelectList(students, "UserId", "DisplayName", SelectedStudentId);
        }
    }
}