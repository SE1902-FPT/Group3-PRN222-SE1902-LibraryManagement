using Group3_SE1902_PRN222_LibraryManagement.Models;
using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

[Authorize(Roles = "Librarian")]
public class BorrowedBooksModel : PageModel
{
    private readonly ThuVienContext _context;
    private readonly IParentNotificationService _parentNotificationService;

    public BorrowedBooksModel(
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

    public List<BorrowedBookRow> CurrentBorrowRecords { get; set; } = new();

    public sealed class BorrowedBookRow
    {
        public int BorrowId { get; set; }
        public string StudentName { get; set; } = "-";
        public string BookTitle { get; set; } = "-";
        public string? CategoryName { get; set; }
        public int CopyId { get; set; }
        public DateTime? BorrowDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string DisplayStatus { get; set; } = "Đang mượn";
        public string StatusBadgeClass { get; set; } = "bg-primary";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await GetCurrentLibrarianAsync();
        if (currentUser == null)
        {
            return RedirectToPage("/Login", new { error = "access_denied" });
        }

        CurrentBorrowRecords = await LoadBorrowedBooksAsync(HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnPostReturnAsync(int borrowId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var currentUser = await GetCurrentLibrarianAsync();
        if (currentUser == null)
        {
            return RedirectToPage("/Login", new { error = "access_denied" });
        }

        var borrowRecord = await _context.BorrowRecords
            .Include(b => b.Copy)
            .FirstOrDefaultAsync(b => b.BorrowId == borrowId, cancellationToken);

        if (borrowRecord == null)
        {
            StatusMessage = "Không tìm thấy phiếu mượn.";
            StatusType = "danger";
            return RedirectToPage();
        }

        if (borrowRecord.ReturnDate != null)
        {
            StatusMessage = "Phiếu mượn này đã được trả trước đó.";
            StatusType = "warning";
            return RedirectToPage();
        }

        if (borrowRecord.Copy == null)
        {
            StatusMessage = "Không tìm thấy bản sách tương ứng.";
            StatusType = "danger";
            return RedirectToPage();
        }

        borrowRecord.ReturnDate = DateTime.Now;
        borrowRecord.Status = "Returned";
        borrowRecord.Copy.Status = "Available";

        await _context.SaveChangesAsync(cancellationToken);
        await _parentNotificationService.PushReturnedNotificationAsync(borrowRecord.BorrowId, cancellationToken);

        StatusMessage = "Đã xác nhận trả sách và gửi thông báo cho phụ huynh.";
        StatusType = "success";
        return RedirectToPage();
    }

    private async Task<List<BorrowedBookRow>> LoadBorrowedBooksAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        var rows = await _context.BorrowRecords
            .AsNoTracking()
            .Where(b => b.ReturnDate == null)
            .Include(b => b.Student)
            .Include(b => b.Copy)
                .ThenInclude(c => c!.Book)
                    .ThenInclude(book => book!.Category)
            .OrderByDescending(b => b.BorrowDate)
            .Select(b => new BorrowedBookRow
            {
                BorrowId = b.BorrowId,
                StudentName = b.Student != null ? b.Student.FullName : "-",
                CopyId = b.CopyId ?? 0,
                BorrowDate = b.BorrowDate,
                DueDate = b.DueDate,
                BookTitle = b.Copy != null && b.Copy.Book != null ? b.Copy.Book.Title : "-",
                CategoryName = b.Copy != null && b.Copy.Book != null && b.Copy.Book.Category != null
                    ? b.Copy.Book.Category.CategoryName
                    : null
            })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            var isOverdue = row.DueDate.HasValue && row.DueDate.Value < now;
            row.DisplayStatus = isOverdue ? "Quá hạn" : "Đang mượn";
            row.StatusBadgeClass = isOverdue ? "bg-danger" : "bg-primary";
        }

        return rows;
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
