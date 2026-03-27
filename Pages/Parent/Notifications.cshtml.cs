using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent
{
    [Authorize(Roles = "Parent")]
    public class NotificationsModel : PageModel
    {
        private readonly ThuVienContext _context;

        public NotificationsModel(ThuVienContext context)
        {
            _context = context;
        }

        public string ParentUserId { get; set; } = "";
        public int UnreadNotificationsCount { get; set; }
        public List<Notification> Notifications { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            if (string.IsNullOrEmpty(ParentUserId)) return RedirectToPage("/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostMarkReadAsync(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif != null)
            {
                // Access check: only allow if it belongs to current user
                var email = User.FindFirstValue(ClaimTypes.Email);
                var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                
                if (parentUser != null && notif.UserId == parentUser.UserId)
                {
                    notif.IsRead = true;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (parentUser != null)
            {
                var unreadList = await _context.Notifications
                    .Where(n => n.UserId == parentUser.UserId && n.IsRead == false)
                    .ToListAsync();

                foreach (var n in unreadList)
                {
                    n.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Role.RoleName == "Parent");
            if (parentUser == null) return;

            ParentUserId = parentUser.UserId.ToString();

            Notifications = await _context.Notifications
                .Where(n => n.UserId == parentUser.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // Limit display to 50 latest
                .ToListAsync();

            UnreadNotificationsCount = Notifications.Count(n => n.IsRead == false);
        }
    }
}
