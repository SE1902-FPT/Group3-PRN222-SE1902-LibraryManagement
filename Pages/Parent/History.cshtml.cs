using Group3_SE1902_PRN222_LibraryManagement.Models;
using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent;

public class HistoryModel : ParentPageModelBase
{
    private readonly ThuVienContext _context;

    public HistoryModel(ThuVienContext context, IParentAccessService parentAccessService)
        : base(parentAccessService)
    {
        _context = context;
    }

    public ParentStudentSummary? SelectedStudent { get; private set; }
    public List<BorrowHistoryRow> BorrowHistory { get; private set; } = new();

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

        BorrowHistory = await _context.VwParentBorrowInfos
            .AsNoTracking()
            .Where(v =>
                v.ParentId == ParentId &&
                v.StudentId == SelectedStudent.StudentId &&
                v.BorrowId != null)
            .OrderByDescending(v => v.BorrowDate)
            .Select(v => new BorrowHistoryRow(
                v.BorrowId!.Value,
                v.BookTitle ?? "Không rõ tên sách",
                v.BorrowDate,
                v.DueDate,
                v.ReturnDate,
                v.BorrowStatus ?? "Không rõ trạng thái"))
            .ToListAsync(cancellationToken);

        return Page();
    }

    public sealed record BorrowHistoryRow(
        int BorrowId,
        string BookTitle,
        DateTime? BorrowDate,
        DateTime? DueDate,
        DateTime? ReturnDate,
        string BorrowStatus);
}
