using Group3_SE1902_PRN222_LibraryManagement.Models;
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
    private readonly Services.NotificationService _notificationService;

    public BorrowedBooksModel(ThuVienContext context, Services.NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public string? ErrorMessage { get; set; }

    public List<BorrowedBookRow> CurrentBorrowRecords { get; set; } = new();

    public class BorrowedBookRow
    {
        public int BorrowId { get; set; }
        public string StudentName { get; set; } = "-";
        public string BookTitle { get; set; } = "-";
        public string? CategoryName { get; set; }
        public int CopyId { get; set; }
        public DateTime? BorrowDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "Borrowed";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            CurrentBorrowRecords = await _context.BorrowRecords
                .Where(b => b.ReturnDate == null)
                .Include(b => b.Student)
                .Include(b => b.Copy)
                    .ThenInclude(c => c!.Book)
                        .ThenInclude(bk => bk!.Category)
                .OrderByDescending(b => b.BorrowDate)
                .Select(b => new BorrowedBookRow
                {
                    BorrowId = b.BorrowId,
                    StudentName = b.Student != null ? b.Student.FullName : "-",
                    CopyId = b.CopyId ?? 0,
                    BorrowDate = b.BorrowDate,
                    DueDate = b.DueDate,
                    Status = b.Status ?? "Borrowed",
                    BookTitle = b.Copy != null && b.Copy.Book != null ? b.Copy.Book.Title : "-",
                    CategoryName = b.Copy != null && b.Copy.Book != null ? b.Copy.Book.Category != null ? b.Copy.Book.Category.CategoryName : null : null
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi lấy dữ liệu: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReturnAsync(int borrowId)
    {
        var record = await _context.BorrowRecords
            .Include(b => b.Copy)
<<<<<<< HEAD
                .ThenInclude(c => c.Book)
            .FirstOrDefaultAsync(b => b.BorrowId == borrowId);

        if (record != null && record.ReturnDate == null)
        {
            record.ReturnDate = DateTime.Now;
            record.Status = "Returned";
            
            if (record.Copy != null)
            {
                record.Copy.Status = "Available";
            }

            // Ghi nhận người trả (thủ thư hiện tại)
            var email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                var librarian = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (librarian != null)
                {
                    record.ProcessedBy = librarian.UserId;
                }
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo realtime cho phụ huynh
            if (record.StudentId.HasValue && record.Copy?.Book?.Title != null)
            {
                await _notificationService.SendReturnAsync(record.StudentId.Value, record.Copy.Book.Title);
            }
        }

=======
            .FirstOrDefaultAsync(b => b.BorrowId == borrowId);

        if (record == null)
        {
            ErrorMessage = "Không tìm thấy bản ghi mượn.";
            await OnGetAsync();
            return Page();
        }

        // 1. Đánh dấu BorrowRecord là đã trả
        record.ReturnDate = DateTime.Now;
        record.Status = "Returned";

        // 2. Đổi BookCopy về Available
        if (record.Copy != null)
            record.Copy.Status = "Available";

        // 3. Cập nhật BorrowRequest tương ứng sang Returned
        var borrowRequest = await _context.BorrowRequests
            .Where(r => r.StudentId == record.StudentId
                     && r.CopyId == record.CopyId
                     && (r.Status == "Borrowed" || r.Status == "Approved"))
            .FirstOrDefaultAsync();

        if (borrowRequest != null)
            borrowRequest.Status = "Returned";

        await _context.SaveChangesAsync();

>>>>>>> 253f800 (update status)
        return RedirectToPage();
    }
}

