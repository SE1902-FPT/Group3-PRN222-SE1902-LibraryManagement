using Group3_SE1902_PRN222_LibraryManagement.Hubs;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Services
{
    /// <summary>
    /// Shared service: inserts a Notification record and pushes it via SignalR to parents.
    /// </summary>
    public class NotificationService
    {
        private readonly ThuVienContext _context;
        private readonly IHubContext<ParentNotificationHub> _hub;

        public NotificationService(ThuVienContext context, IHubContext<ParentNotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // ─── Called when librarian APPROVES a borrow request ───────────────────
        public async Task SendBorrowAsync(int studentId, string bookTitle, DateTime dueDate)
        {
            var parents = await GetParentsAsync(studentId);
            if (!parents.Any()) return;

            var student = await _context.Users.FindAsync(studentId);
            var studentName = student?.FullName ?? "Học sinh";

            foreach (var parent in parents)
            {
                var message = $"Con bạn ({studentName}) đã mượn sách \"{bookTitle}\". Hạn trả: {dueDate:dd/MM/yyyy}.";
                await InsertAndPushAsync(parent.UserId, message);
            }
        }

        // ─── Called when librarian processes a return ───────────────────────────
        public async Task SendReturnAsync(int studentId, string bookTitle)
        {
            var parents = await GetParentsAsync(studentId);
            if (!parents.Any()) return;

            var student = await _context.Users.FindAsync(studentId);
            var studentName = student?.FullName ?? "Học sinh";

            foreach (var parent in parents)
            {
                var message = $"Con bạn ({studentName}) đã trả sách \"{bookTitle}\".";
                await InsertAndPushAsync(parent.UserId, message);
            }
        }

        // ─── Called by the overdue job ──────────────────────────────────────────
        public async Task SendOverdueAsync(int parentId, string studentName, string bookTitle)
        {
            var message = $"Sách \"{bookTitle}\" của con bạn ({studentName}) đã quá hạn trả. Vui lòng nhắc con trả sách sớm.";
            await InsertAndPushAsync(parentId, message);
        }

        // ─── Duplicate-detection for overdue (no extra DB column) ──────────────
        public bool OverdueNotificationExists(int parentId, string studentName, string bookTitle)
        {
            return _context.Notifications.Any(n =>
                n.UserId == parentId &&
                n.Message != null &&
                n.Message.Contains(studentName) &&
                n.Message.Contains(bookTitle) &&
                n.Message.Contains("quá hạn"));
        }

        // ─── Private helpers ────────────────────────────────────────────────────
        private async Task InsertAndPushAsync(int parentId, string message)
        {
            var notification = new Notification
            {
                UserId = parentId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Push real-time to parent group (only if online; SignalR silently ignores offline)
            await _hub.Clients.Group($"parent_{parentId}").SendAsync("ReceiveNotification", new
            {
                message,
                createdAt = notification.CreatedAt?.ToString("dd/MM/yyyy HH:mm")
            });
        }

        private async Task<List<User>> GetParentsAsync(int studentId)
        {
            return await _context.ParentStudents
                .Where(ps => ps.StudentId == studentId)
                .Select(ps => ps.Parent)
                .ToListAsync();
        }
    }
}
