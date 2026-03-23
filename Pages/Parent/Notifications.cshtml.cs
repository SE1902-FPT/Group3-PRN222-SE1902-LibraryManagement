using Group3_SE1902_PRN222_LibraryManagement.Models;
using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent;

public class NotificationsModel : ParentPageModelBase
{
    private readonly ThuVienContext _context;

    public NotificationsModel(ThuVienContext context, IParentAccessService parentAccessService)
        : base(parentAccessService)
    {
        _context = context;
    }

    public List<NotificationRow> NotificationItems { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var cancellationToken = HttpContext.RequestAborted;
        if (!await LoadParentContextAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        await LoadNotificationsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int notificationId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        if (!await LoadParentContextAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken);

        if (notification == null || notification.UserId != ParentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToPage();
    }

    private async Task LoadNotificationsAsync(CancellationToken cancellationToken)
    {
        NotificationItems = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == ParentId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationRow(
                n.NotificationId,
                n.Message ?? string.Empty,
                n.IsRead ?? false,
                n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public sealed record NotificationRow(
        int NotificationId,
        string Message,
        bool IsRead,
        DateTime? CreatedAt);
}
