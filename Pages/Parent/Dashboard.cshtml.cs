using Group3_SE1902_PRN222_LibraryManagement.Models;
using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent;

public class DashboardModel : ParentPageModelBase
{
    private readonly ThuVienContext _context;

    public DashboardModel(ThuVienContext context, IParentAccessService parentAccessService)
        : base(parentAccessService)
    {
        _context = context;
    }

    public ParentStudentSummary? SelectedStudent { get; private set; }
    public DashboardSummary? Summary { get; private set; }
    public List<CurrentBorrowedBookRow> CurrentBorrowedBooks { get; private set; } = new();

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

        var currentBorrowedCount = await _context.BorrowRecords
            .AsNoTracking()
            .CountAsync(br => br.StudentId == SelectedStudent.StudentId && br.ReturnDate == null, cancellationToken);

        var totalBorrowCount = await _context.BorrowRecords
            .AsNoTracking()
            .CountAsync(br => br.StudentId == SelectedStudent.StudentId, cancellationToken);

        CurrentBorrowedBooks = await _context.VwParentBorrowInfos
            .AsNoTracking()
            .Where(v =>
                v.ParentId == ParentId &&
                v.StudentId == SelectedStudent.StudentId &&
                v.BorrowId != null &&
                v.ReturnDate == null)
            .OrderBy(v => v.DueDate)
            .Select(v => new CurrentBorrowedBookRow(
                v.BookTitle ?? "Không rõ tên sách",
                v.BorrowDate,
                v.DueDate,
                v.BorrowStatus ?? "Đang mượn"))
            .Take(5)
            .ToListAsync(cancellationToken);

        Summary = new DashboardSummary(
            currentBorrowedCount,
            totalBorrowCount,
            UnreadNotificationCount);

        return Page();
    }

    public sealed record DashboardSummary(
        int CurrentBorrowedCount,
        int TotalBorrowCount,
        int UnreadNotificationCount);

    public sealed record CurrentBorrowedBookRow(
        string BookTitle,
        DateTime? BorrowDate,
        DateTime? DueDate,
        string BorrowStatus);
}
