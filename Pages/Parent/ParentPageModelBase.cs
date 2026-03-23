using Group3_SE1902_PRN222_LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Parent;

[Authorize]
public abstract class ParentPageModelBase : PageModel
{
    private readonly IParentAccessService _parentAccessService;

    protected ParentPageModelBase(IParentAccessService parentAccessService)
    {
        _parentAccessService = parentAccessService;
    }

    public int ParentId { get; private set; }
    public string ParentName { get; private set; } = string.Empty;
    public IReadOnlyList<ParentStudentSummary> StudentOptions { get; private set; } = Array.Empty<ParentStudentSummary>();
    public int UnreadNotificationCount { get; private set; }
    public bool RequiresStudentSelection => StudentOptions.Count > 1;

    protected async Task<bool> LoadParentContextAsync(CancellationToken cancellationToken = default)
    {
        var parent = await _parentAccessService.GetCurrentParentAsync(User, cancellationToken);
        if (parent == null)
        {
            return false;
        }

        ParentId = parent.UserId;
        ParentName = parent.FullName;
        StudentOptions = await _parentAccessService.GetChildrenAsync(parent.UserId, cancellationToken);
        UnreadNotificationCount = await _parentAccessService.GetUnreadNotificationCountAsync(parent.UserId, cancellationToken);

        ViewData["ParentName"] = ParentName;
        ViewData["UnreadNotificationCount"] = UnreadNotificationCount;

        return true;
    }

    protected IActionResult? ResolveSelectedStudent(int? studentId, out ParentStudentSummary? selectedStudent)
    {
        selectedStudent = null;

        if (StudentOptions.Count == 0)
        {
            return null;
        }

        if (StudentOptions.Count == 1)
        {
            var onlyChild = StudentOptions[0];
            if (studentId.HasValue && studentId.Value != onlyChild.StudentId)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            selectedStudent = onlyChild;
            return null;
        }

        if (!studentId.HasValue)
        {
            return null;
        }

        selectedStudent = StudentOptions.FirstOrDefault(s => s.StudentId == studentId.Value);
        return selectedStudent == null
            ? StatusCode(StatusCodes.Status403Forbidden)
            : null;
    }
}
