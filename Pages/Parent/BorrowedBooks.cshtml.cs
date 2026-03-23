using Group3_SE1902_PRN222_LibraryManagement.Models;
using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent;

public class BorrowedBooksModel : ParentPageModelBase
{
    private readonly ThuVienContext _context;

    public BorrowedBooksModel(ThuVienContext context, IParentAccessService parentAccessService)
        : base(parentAccessService)
    {
        _context = context;
    }

    public ParentStudentSummary? SelectedStudent { get; private set; }
    public List<CurrentBorrowedBookRow> BorrowedBooks { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int? studentId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        if (!await LoadParentContextAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var resolution = ResolveSelectedStudent(studentId, out var selectedStudent);
        if (resolution != null)
        {
            return resolution;
        }

        SelectedStudent = selectedStudent;
        if (SelectedStudent == null)
        {
            return Page();
        }

        BorrowedBooks = await _context.VwParentBorrowInfos
            .AsNoTracking()
            .Where(v =>
                v.ParentId == ParentId &&
                v.StudentId == SelectedStudent.StudentId &&
                v.BorrowId != null &&
                v.ReturnDate == null)
            .OrderBy(v => v.DueDate)
            .Select(v => new CurrentBorrowedBookRow(
                v.BorrowId!.Value,
                v.BookTitle ?? "Không rõ tên sách",
                v.BorrowDate,
                v.DueDate,
                v.BorrowStatus ?? "Đang mượn"))
            .ToListAsync(cancellationToken);

        return Page();
    }

    public sealed record CurrentBorrowedBookRow(
        int BorrowId,
        string BookTitle,
        DateTime? BorrowDate,
        DateTime? DueDate,
        string BorrowStatus);
}
