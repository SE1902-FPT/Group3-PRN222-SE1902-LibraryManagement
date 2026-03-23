using Group3_SE1902_PRN222_LibraryManagement.Models;
using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

[Authorize(Roles = "Librarian")]
public class BorrowRequestsModel : PageModel
{
    private readonly ThuVienContext _context;
    private readonly IParentNotificationService _parentNotificationService;

    public BorrowRequestsModel(
        ThuVienContext context,
        IParentNotificationService parentNotificationService)
    {
        _context = context;
        _parentNotificationService = parentNotificationService;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusType { get; set; }

    public int? LibrarianId { get; set; }
    public string? LibrarianName { get; set; }
    public List<BorrowRequestRow> PendingRequests { get; set; } = new();

    public sealed class BorrowRequestRow
    {
        public int RequestId { get; set; }
        public DateTime? RequestDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public string StudentName { get; set; } = "-";
        public string BookTitle { get; set; } = "-";
        public string? CategoryName { get; set; }
        public int CopyId { get; set; }
        public string RequestStatus { get; set; } = "Pending";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await GetCurrentLibrarianAsync();
        if (currentUser == null)
        {
            return RedirectToPage("/Login", new { error = "access_denied" });
        }

        LibrarianId = currentUser.UserId;
        LibrarianName = currentUser.FullName;
        PendingRequests = await LoadPendingRequestsAsync(HttpContext.RequestAborted);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int requestId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var currentUser = await GetCurrentLibrarianAsync();
        if (currentUser == null)
        {
            return RedirectToPage("/Login", new { error = "access_denied" });
        }

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var request = await _context.BorrowRequests
            .Include(r => r.Copy)
            .FirstOrDefaultAsync(r => r.RequestId == requestId, cancellationToken);

        if (request == null)
        {
            StatusMessage = "Không tìm thấy yêu cầu mượn.";
            StatusType = "danger";
            return RedirectToPage();
        }

        if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Yêu cầu này không còn ở trạng thái chờ duyệt.";
            StatusType = "warning";
            return RedirectToPage();
        }

        if (request.Copy == null)
        {
            StatusMessage = "Yêu cầu không có bản sách hợp lệ.";
            StatusType = "danger";
            return RedirectToPage();
        }

        if (!string.Equals(request.Copy.Status, "Available", StringComparison.OrdinalIgnoreCase))
        {
            request.Status = "Rejected";
            request.ApprovedBy = currentUser.UserId;
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            StatusMessage = "Bản sách không còn sẵn sàng, yêu cầu đã được từ chối.";
            StatusType = "warning";
            return RedirectToPage();
        }

        var now = DateTime.Now;
        var dueDate = request.ExpectedReturnDate.HasValue && request.ExpectedReturnDate.Value > now
            ? request.ExpectedReturnDate.Value
            : now.AddDays(14);

        request.Status = "Approved";
        request.ApprovedBy = currentUser.UserId;

        var borrowRecord = new BorrowRecord
        {
            StudentId = request.StudentId,
            CopyId = request.Copy.CopyId,
            BorrowDate = now,
            DueDate = dueDate,
            ReturnDate = null,
            Status = "Borrowed",
            ProcessedBy = currentUser.UserId
        };

        request.Copy.Status = "Borrowed";
        _context.BorrowRecords.Add(borrowRecord);

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await _parentNotificationService.PushBorrowedNotificationAsync(borrowRecord.BorrowId, cancellationToken);

        StatusMessage = "Đã duyệt yêu cầu mượn và gửi thông báo cho phụ huynh.";
        StatusType = "success";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int requestId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var currentUser = await GetCurrentLibrarianAsync();
        if (currentUser == null)
        {
            return RedirectToPage("/Login", new { error = "access_denied" });
        }

        var request = await _context.BorrowRequests
            .FirstOrDefaultAsync(r => r.RequestId == requestId, cancellationToken);

        if (request == null)
        {
            StatusMessage = "Không tìm thấy yêu cầu mượn.";
            StatusType = "danger";
            return RedirectToPage();
        }

        if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Yêu cầu này không còn ở trạng thái chờ duyệt.";
            StatusType = "warning";
            return RedirectToPage();
        }

        request.Status = "Rejected";
        request.ApprovedBy = currentUser.UserId;
        await _context.SaveChangesAsync(cancellationToken);

        StatusMessage = "Đã từ chối yêu cầu mượn.";
        StatusType = "success";
        return RedirectToPage();
    }

    private async Task<List<BorrowRequestRow>> LoadPendingRequestsAsync(CancellationToken cancellationToken)
    {
        return await _context.BorrowRequests
            .AsNoTracking()
            .Where(r => r.Status == "Pending")
            .Include(r => r.Student)
            .Include(r => r.Copy)
                .ThenInclude(c => c!.Book)
                    .ThenInclude(b => b!.Category)
            .OrderByDescending(r => r.RequestDate)
            .Select(r => new BorrowRequestRow
            {
                RequestId = r.RequestId,
                RequestDate = r.RequestDate,
                ExpectedReturnDate = r.ExpectedReturnDate,
                StudentName = r.Student != null ? r.Student.FullName : "-",
                BookTitle = r.Copy != null && r.Copy.Book != null ? r.Copy.Book.Title : "-",
                CategoryName = r.Copy != null && r.Copy.Book != null && r.Copy.Book.Category != null
                    ? r.Copy.Book.Category.CategoryName
                    : null,
                CopyId = r.Copy != null ? r.Copy.CopyId : 0,
                RequestStatus = r.Status ?? "Pending"
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<User?> GetCurrentLibrarianAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.RoleId == 4);
    }
}
