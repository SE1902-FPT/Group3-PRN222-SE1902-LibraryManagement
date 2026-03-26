using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent
{
    [Authorize(Roles = "Parent")]
    public class DashboardModel : PageModel
    {
        private readonly ThuVienContext _context;

        public DashboardModel(ThuVienContext context)
        {
            _context = context;
        }

        public string ParentUserId { get; set; } = "";
        public int UnreadNotificationsCount { get; set; }
        public List<ChildInfo> Children { get; set; } = new();
        public int? SelectedStudentId { get; set; }
        public StudentStats Stats { get; set; } = new();
        public List<VwParentBorrowInfo> RecentBorrows { get; set; } = new();

        public class ChildInfo
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; } = "";
            public string ClassName { get; set; } = "";
        }

        public class StudentStats
        {
            public int CurrentlyBorrowed { get; set; }
            public int TotalBorrowed { get; set; }
            public int Overdue { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? studentId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Role.RoleName == "Parent");
            if (parentUser == null) return RedirectToPage("/Login");

            ParentUserId = parentUser.UserId.ToString();

            // Lấy danh sách con
            var studentLinks = await _context.ParentStudents
                .Where(ps => ps.ParentId == parentUser.UserId)
                .Include(ps => ps.Student)
                    .ThenInclude(s => s.ClassesNavigation)
                .ToListAsync();

            Children = studentLinks.Select(ps => new ChildInfo
            {
                StudentId = ps.StudentId,
                StudentName = ps.Student.FullName,
                ClassName = ps.Student.ClassesNavigation.FirstOrDefault()?.ClassName ?? "N/A"
            }).ToList();

            if (!Children.Any()) return Page();

            // Auto select
            SelectedStudentId = studentId;
            if (SelectedStudentId == null || !Children.Any(c => c.StudentId == SelectedStudentId))
            {
                SelectedStudentId = Children.First().StudentId;
            }

            // Unread notifications
            UnreadNotificationsCount = await _context.Notifications
                .CountAsync(n => n.UserId == parentUser.UserId && n.IsRead == false);

            // Stats cho SelectedStudentId
            var allBorrows = await _context.VwParentBorrowInfos
                .Where(v => v.ParentId == parentUser.UserId && v.StudentId == SelectedStudentId)
                .ToListAsync();

            var now = DateTime.Now;
            Stats.TotalBorrowed = allBorrows.Count;
            Stats.CurrentlyBorrowed = allBorrows.Count(b => b.ReturnDate == null);
            Stats.Overdue = allBorrows.Count(b => b.ReturnDate == null && b.DueDate < now);

            // Gần đây (đang mượn)
            RecentBorrows = allBorrows
                .Where(b => b.ReturnDate == null)
                .OrderBy(b => b.DueDate)
                .Take(5)
                .ToList();

            return Page();
        }
    }
}
