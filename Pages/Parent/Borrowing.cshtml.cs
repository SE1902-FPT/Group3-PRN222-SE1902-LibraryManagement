using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent
{
    [Authorize(Roles = "Parent")]
    public class BorrowingModel : PageModel
    {
        private readonly ThuVienContext _context;

        public BorrowingModel(ThuVienContext context)
        {
            _context = context;
        }

        public string ParentUserId { get; set; } = "";
        public int UnreadNotificationsCount { get; set; }
        public List<ChildInfo> Children { get; set; } = new();
        public int? SelectedStudentId { get; set; }
        
        public List<VwParentBorrowInfo> BorrowRecords { get; set; } = new();

        public class ChildInfo
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync(int? studentId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Role.RoleName == "Parent");
            if (parentUser == null) return RedirectToPage("/Login");

            ParentUserId = parentUser.UserId.ToString();

            // Navigation info
            UnreadNotificationsCount = await _context.Notifications
                .CountAsync(n => n.UserId == parentUser.UserId && n.IsRead == false);

            var studentLinks = await _context.ParentStudents
                .Where(ps => ps.ParentId == parentUser.UserId)
                .Include(ps => ps.Student)
                .ToListAsync();

            Children = studentLinks.Select(ps => new ChildInfo
            {
                StudentId = ps.StudentId,
                StudentName = ps.Student.FullName
            }).ToList();

            if (!Children.Any()) return Page();

            // Nếu người dùng chuyền studentId, kiểm tra quyền sở hữu (HTTP 403 nếu cố tình sửa ID)
            if (studentId.HasValue && !Children.Any(c => c.StudentId == studentId.Value))
            {
                // Trả về 403 Forbidden nếu studentId không thuộc quyền
                return Forbid();
            }

            SelectedStudentId = studentId;

            // Query ReturnDate IS NULL (Đang mượn)
            var query = _context.VwParentBorrowInfos
                .Where(v => v.ParentId == parentUser.UserId && v.ReturnDate == null);

            if (SelectedStudentId.HasValue)
            {
                query = query.Where(v => v.StudentId == SelectedStudentId.Value);
            }

            BorrowRecords = await query.OrderBy(v => v.DueDate).ToListAsync();

            return Page();
        }
    }
}
