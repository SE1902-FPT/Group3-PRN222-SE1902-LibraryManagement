using System.ComponentModel.DataAnnotations;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

public class BorrowRequestsModel : PageModel
{
    private readonly ThuVienContext _context;

    public BorrowRequestsModel(ThuVienContext context)
    {
        _context = context;
    }

    private const int LibrarianRoleId = 4;

    public int? LibrarianId { get; set; }
    public string? LibrarianName { get; set; }
    public string? ErrorMessage { get; set; }

    public List<BorrowRequestRow> PendingRequests { get; set; } = new();

    public class BorrowRequestRow
    {
        public int RequestId { get; set; }
        public DateTime? RequestDate { get; set; }
        public string StudentName { get; set; } = "-";
        public string BookTitle { get; set; } = "-";
        public string? CategoryName { get; set; }
        public int CopyId { get; set; }
        public string RequestStatus { get; set; } = "Pending";
    }

    private async Task<bool> ResolveLibrarianAsync(int? librarianId)
    {
        if (librarianId.HasValue)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == librarianId.Value && u.RoleId == LibrarianRoleId);
            if (user == null) return false;
            LibrarianId = user.UserId;
            LibrarianName = user.FullName;
            return true;
        }

        var first = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == LibrarianRoleId);
        if (first == null) return false;
        LibrarianId = first.UserId;
        LibrarianName = first.FullName;
        return true;
    }

    public async Task OnGetAsync(int? librarianId)
    {
        var ok = await ResolveLibrarianAsync(librarianId);
        if (!ok)
        {
            ErrorMessage = "Không xác định được Librarian (RoleId = 4).";
            PendingRequests.Clear();
            return;
        }

        PendingRequests = await _context.BorrowRequests
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
                StudentName = r.Student != null ? r.Student.FullName : "-",
                BookTitle = r.Copy != null && r.Copy.Book != null ? r.Copy.Book.Title : "-",
                CategoryName = r.Copy != null && r.Copy.Book != null ? r.Copy.Book.Category != null ? r.Copy.Book.Category.CategoryName : null : null,
                CopyId = r.Copy != null ? r.Copy.CopyId : 0,
                RequestStatus = r.Status ?? "Pending"
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int requestId, int librarianId)
    {
        ErrorMessage = null;

        var ok = await ResolveLibrarianAsync(librarianId);
        if (!ok)
        {
            ErrorMessage = "librarianId không hợp lệ (không phải Librarian).";
            return Page();
        }

        using var tx = await _context.Database.BeginTransactionAsync();

        var request = await _context.BorrowRequests
            .Include(r => r.Copy)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null)
        {
            ErrorMessage = "Không tìm thấy request.";
            return Page();
        }

        if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Request này không còn ở trạng thái Pending.";
            return Page();
        }

        if (request.Copy == null)
        {
            ErrorMessage = "Request không có BookCopy hợp lệ.";
            return Page();
        }

        if (!string.Equals(request.Copy.Status, "Available", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Sách/bản này hiện không còn Available.";
            request.Status = "Rejected";
            request.ApprovedBy = LibrarianId;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Page();
        }

        request.Status = "Approved";
        request.ApprovedBy = LibrarianId;

        // Tạo record mượn mới
        var now = DateTime.Now;
        var dueDate = now.AddDays(14); // Mặc định 14 ngày cho học sinh

        var borrowRecord = new BorrowRecord
        {
            StudentId = request.StudentId,
            CopyId = request.Copy.CopyId,
            BorrowDate = now,
            DueDate = dueDate,
            ReturnDate = null,
            Status = "Borrowed",
            ProcessedBy = LibrarianId
        };

        request.Copy.Status = "Borrowed";

        _context.BorrowRecords.Add(borrowRecord);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return RedirectToPage("/Librarian/BorrowRequests", new { librarianId = LibrarianId });
    }

    public async Task<IActionResult> OnPostRejectAsync(int requestId, int librarianId)
    {
        ErrorMessage = null;

        var ok = await ResolveLibrarianAsync(librarianId);
        if (!ok)
        {
            ErrorMessage = "librarianId không hợp lệ (không phải Librarian).";
            return Page();
        }

        var request = await _context.BorrowRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null)
        {
            ErrorMessage = "Không tìm thấy request.";
            return Page();
        }

        if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Request này không còn ở trạng thái Pending.";
            return Page();
        }

        request.Status = "Rejected";
        request.ApprovedBy = LibrarianId;
        await _context.SaveChangesAsync();

        return RedirectToPage("/Librarian/BorrowRequests", new { librarianId = LibrarianId });
    }
}

