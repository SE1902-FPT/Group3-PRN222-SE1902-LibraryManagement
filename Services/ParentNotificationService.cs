using Group3_SE1902_PRN222_LibraryManagement.Hubs;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Services;

public interface IParentNotificationService
{
    Task PushBorrowedNotificationAsync(int borrowId, CancellationToken cancellationToken = default);
    Task PushReturnedNotificationAsync(int borrowId, CancellationToken cancellationToken = default);
    Task<int> CreateAndPushOverdueNotificationsAsync(CancellationToken cancellationToken = default);
}

public sealed class ParentNotificationService : IParentNotificationService
{
    private const string OverdueKeyword = "quá hạn";

    private readonly ThuVienContext _context;
    private readonly IHubContext<ParentNotificationHub> _hubContext;
    private readonly ILogger<ParentNotificationService> _logger;

    public ParentNotificationService(
        ThuVienContext context,
        IHubContext<ParentNotificationHub> hubContext,
        ILogger<ParentNotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PushBorrowedNotificationAsync(int borrowId, CancellationToken cancellationToken = default)
    {
        var borrowInfo = await LoadBorrowEventInfoAsync(borrowId, cancellationToken);
        if (borrowInfo == null)
        {
            return;
        }

        var message = BuildBorrowedMessage(borrowInfo.StudentName, borrowInfo.BookTitle, borrowInfo.DueDate);
        await PushExistingNotificationsAsync(borrowInfo.ParentIds, message, cancellationToken);
    }

    public async Task PushReturnedNotificationAsync(int borrowId, CancellationToken cancellationToken = default)
    {
        var borrowInfo = await LoadBorrowEventInfoAsync(borrowId, cancellationToken);
        if (borrowInfo == null)
        {
            return;
        }

        var message = BuildReturnedMessage(borrowInfo.StudentName, borrowInfo.BookTitle);
        await PushExistingNotificationsAsync(borrowInfo.ParentIds, message, cancellationToken);
    }

    public async Task<int> CreateAndPushOverdueNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;

        var overdueRecords = await _context.BorrowRecords
            .AsNoTracking()
            .Where(br =>
                br.ReturnDate == null &&
                br.DueDate != null &&
                br.DueDate < now &&
                br.StudentId != null &&
                br.Student != null &&
                br.Copy != null &&
                br.Copy.Book != null)
            .Select(br => new OverdueBorrowInfo(
                br.StudentId!.Value,
                br.Student!.FullName,
                br.Copy!.Book!.Title))
            .ToListAsync(cancellationToken);

        if (overdueRecords.Count == 0)
        {
            return 0;
        }

        var studentIds = overdueRecords
            .Select(r => r.StudentId)
            .Distinct()
            .ToList();

        var parentLinks = await _context.ParentStudents
            .AsNoTracking()
            .Where(ps => studentIds.Contains(ps.StudentId))
            .Select(ps => new { ps.StudentId, ps.ParentId })
            .ToListAsync(cancellationToken);

        var parentMap = parentLinks
            .GroupBy(link => link.StudentId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.ParentId).Distinct().ToList());

        var createdNotifications = new List<Notification>();

        foreach (var overdueRecord in overdueRecords)
        {
            if (!parentMap.TryGetValue(overdueRecord.StudentId, out var parentIds))
            {
                continue;
            }

            foreach (var parentId in parentIds)
            {
                var alreadyExists = await _context.Notifications
                    .AsNoTracking()
                    .AnyAsync(n =>
                        n.UserId == parentId &&
                        n.Message != null &&
                        n.Message.Contains(overdueRecord.StudentName) &&
                        n.Message.Contains(overdueRecord.BookTitle) &&
                        n.Message.Contains(OverdueKeyword),
                        cancellationToken);

                if (alreadyExists)
                {
                    continue;
                }

                createdNotifications.Add(new Notification
                {
                    UserId = parentId,
                    Message = BuildOverdueMessage(overdueRecord.StudentName, overdueRecord.BookTitle),
                    IsRead = false,
                    CreatedAt = now
                });
            }
        }

        if (createdNotifications.Count == 0)
        {
            return 0;
        }

        _context.Notifications.AddRange(createdNotifications);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var notification in createdNotifications)
        {
            if (notification.UserId == null)
            {
                continue;
            }

            await SendRealtimeAsync(ToRealtimePayload(notification), cancellationToken);
        }

        return createdNotifications.Count;
    }

    private async Task PushExistingNotificationsAsync(
        IReadOnlyCollection<int> parentIds,
        string message,
        CancellationToken cancellationToken)
    {
        if (parentIds.Count == 0)
        {
            return;
        }

        var lookupWindow = DateTime.Now.AddMinutes(-5);

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n =>
                n.UserId != null &&
                parentIds.Contains(n.UserId.Value) &&
                n.Message == message &&
                n.CreatedAt != null &&
                n.CreatedAt >= lookupWindow)
            .OrderByDescending(n => n.NotificationId)
            .ToListAsync(cancellationToken);

        foreach (var parentId in parentIds.Distinct())
        {
            var notification = notifications.FirstOrDefault(n => n.UserId == parentId);
            var payload = notification != null
                ? ToRealtimePayload(notification)
                : CreateFallbackPayload(parentId, message);

            await SendRealtimeAsync(payload, cancellationToken);
        }
    }

    private async Task<BorrowEventInfo?> LoadBorrowEventInfoAsync(int borrowId, CancellationToken cancellationToken)
    {
        var borrowInfo = await _context.BorrowRecords
            .AsNoTracking()
            .Where(br =>
                br.BorrowId == borrowId &&
                br.StudentId != null &&
                br.Student != null &&
                br.Copy != null &&
                br.Copy.Book != null)
            .Select(br => new BorrowEventInfo(
                br.StudentId!.Value,
                br.Student!.FullName,
                br.Copy!.Book!.Title,
                br.DueDate,
                Array.Empty<int>()))
            .FirstOrDefaultAsync(cancellationToken);

        if (borrowInfo == null)
        {
            return null;
        }

        var parentIds = await _context.ParentStudents
            .AsNoTracking()
            .Where(ps => ps.StudentId == borrowInfo.StudentId)
            .Select(ps => ps.ParentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return borrowInfo with { ParentIds = parentIds };
    }

    private async Task SendRealtimeAsync(RealtimeNotificationPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients
                .User(payload.UserId.ToString())
                .SendAsync("ReceiveNotification", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi realtime notification tới parent {ParentId}.", payload.UserId);
        }
    }

    private static string BuildBorrowedMessage(string studentName, string bookTitle, DateTime? dueDate)
    {
        var deadline = dueDate?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
        return $"Học sinh {studentName} đã mượn sách \"{bookTitle}\". Hạn trả: {deadline}";
    }

    private static string BuildReturnedMessage(string studentName, string bookTitle)
        => $"Học sinh {studentName} đã trả sách \"{bookTitle}\"";

    private static string BuildOverdueMessage(string studentName, string bookTitle)
        => $"Học sinh {studentName} đã quá hạn trả sách \"{bookTitle}\"";

    private static RealtimeNotificationPayload ToRealtimePayload(Notification notification)
    {
        var createdAt = notification.CreatedAt ?? DateTime.Now;
        return new RealtimeNotificationPayload(
            notification.NotificationId,
            notification.UserId ?? 0,
            notification.Message ?? string.Empty,
            notification.IsRead ?? false,
            createdAt,
            createdAt.ToString("dd/MM/yyyy HH:mm"));
    }

    private static RealtimeNotificationPayload CreateFallbackPayload(int parentId, string message)
    {
        var now = DateTime.Now;
        return new RealtimeNotificationPayload(
            0,
            parentId,
            message,
            false,
            now,
            now.ToString("dd/MM/yyyy HH:mm"));
    }

    private sealed record BorrowEventInfo(
        int StudentId,
        string StudentName,
        string BookTitle,
        DateTime? DueDate,
        IReadOnlyCollection<int> ParentIds);

    private sealed record OverdueBorrowInfo(
        int StudentId,
        string StudentName,
        string BookTitle);
}

public sealed record RealtimeNotificationPayload(
    int NotificationId,
    int UserId,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    string CreatedAtDisplay);
