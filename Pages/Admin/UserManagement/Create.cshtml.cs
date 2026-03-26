using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin.UserManagement
{
    public class CreateModel : PageModel
    {
        private readonly ThuVienContext _context;

        public CreateModel(ThuVienContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User User { get; set; } = default!;

        [BindProperty]
        public string RawPassword { get; set; } = "123456";

        [BindProperty]
        public int? SelectedClassId { get; set; }

        [BindProperty]
        public string? Relationship { get; set; }

        [BindProperty]
        public string? ParentPhone { get; set; }

        [BindProperty]
        public string? ParentAddress { get; set; }

        public IActionResult OnGet()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // BƯỚC 1: Xóa sạch Validation của các bảng liên quan (Tránh lỗi ModelState.IsValid = false)
            var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("User.") && k != "User.FullName" && k != "User.Email" && k != "User.RoleId").ToList();
            foreach (var key in keysToRemove) ModelState.Remove(key);

            if (!ModelState.IsValid)
            {
                // Debug: Xem trường nào đang báo lỗi nếu ModelState vẫn False
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors) Console.WriteLine("Validation Error: " + error);

                ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
                ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName");
                return Page();
            }

            try
            {
                // BƯỚC 2: Gán các giá trị bắt buộc mà DB yêu cầu
                User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(RawPassword);
                User.CreatedAt = DateTime.Now;
                User.Status = "Active";

                // Đảm bảo các trường string không bị null nếu DB không cho phép
                User.FullName = User.FullName ?? "";
                User.Email = User.Email ?? "";

                _context.Users.Add(User);
                await _context.SaveChangesAsync(); // Lưu lần 1 để có UserId

                // BƯỚC 3: Xử lý Logic Role
                var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == User.RoleId);

                if (role?.RoleName == "Student" && SelectedClassId.HasValue)
                {
                    var cls = await _context.Classes.Include(c => c.Students).FirstOrDefaultAsync(c => c.ClassId == SelectedClassId);
                    if (cls != null)
                    {
                        cls.Students.Add(User);
                        await _context.SaveChangesAsync();
                    }
                }
                else if (role?.RoleName == "Teacher" && SelectedClassId.HasValue)
                {
                    var cls = await _context.Classes.FirstOrDefaultAsync(c => c.ClassId == SelectedClassId);
                    if (cls != null)
                    {
                        cls.TeacherId = User.UserId;
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToPage("./Index");
            }
            catch (DbUpdateException dbEx)
            {
                // LỖI DATABASE (Ví dụ: Trùng Email, Sai Foreign Key)
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                ModelState.AddModelError(string.Empty, "Lỗi Database: " + innerMessage);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống: " + ex.Message);
            }

            // Load lại dữ liệu nếu có lỗi
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName");
            return Page();
        }
    }
}