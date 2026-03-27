using Group3_SE1902_PRN222_LibraryManagement.Models;
using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Services
{
    /// <summary>
    /// Background job that checks for overdue borrow records every 5 minutes
    /// and sends notifications to parents via NotificationService.
    /// Uses IServiceScopeFactory so it can resolve scoped EF context.
    /// </summary>
    public class OverdueCheckJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverdueCheckJob> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public OverdueCheckJob(IServiceScopeFactory scopeFactory, ILogger<OverdueCheckJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OverdueCheckJob started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckOverdueAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OverdueCheckJob.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CheckOverdueAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ThuVienContext>();
            var notifService = scope.ServiceProvider.GetRequiredService<NotificationService>();

            var now = DateTime.Now;

            // Find all overdue, not-yet-returned records
            var overdueRecords = await context.BorrowRecords
                .Where(b => b.ReturnDate == null && b.DueDate < now)
                .Include(b => b.Student)
                .Include(b => b.Copy)
                    .ThenInclude(c => c!.Book)
                .ToListAsync(ct);

            foreach (var record in overdueRecords)
            {
                if (record.Student == null || record.Copy?.Book == null) continue;

                var studentName = record.Student.FullName;
                var bookTitle = record.Copy.Book.Title ?? "Không rõ";

                // Find all parents of this student
                var parentLinks = await context.ParentStudents
                    .Where(ps => ps.StudentId == record.StudentId)
                    .Select(ps => ps.ParentId)
                    .ToListAsync(ct);

                foreach (var parentId in parentLinks)
                {
                    // Avoid sending duplicate overdue notifications (no extra column approach)
                    if (!notifService.OverdueNotificationExists(parentId, studentName, bookTitle))
                    {
                        await notifService.SendOverdueAsync(parentId, studentName, bookTitle);
                        _logger.LogInformation("Sent overdue notification to parent {ParentId} for student {Student} / book {Book}",
                            parentId, studentName, bookTitle);
                    }
                }
            }
        }
    }
}
