using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Librarian;

public class BorrowedBooksModel : PageModel
{
    private readonly ThuVienContext _context;

    public BorrowedBooksModel(ThuVienContext context)
    {
        _context = context;
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

    public async Task OnGetAsync()
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
    }
}

