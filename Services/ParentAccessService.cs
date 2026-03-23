using System.Security.Claims;
using Group3_SE1902_PRN222_LibraryManagement.Extensions;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Services;

public interface IParentAccessService
{
    Task<ParentIdentity?> GetCurrentParentAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<List<ParentStudentSummary>> GetChildrenAsync(int parentId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadNotificationCountAsync(int parentId, CancellationToken cancellationToken = default);
}

public sealed class ParentAccessService : IParentAccessService
{
    private readonly ThuVienContext _context;

    public ParentAccessService(ThuVienContext context)
    {
        _context = context;
    }

    public async Task<ParentIdentity?> GetCurrentParentAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole("Parent"))
        {
            return null;
        }

        var userId = user.GetUserId();
        if (userId.HasValue)
        {
            var parentById = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId.Value && u.RoleId == 2)
                .Select(u => new ParentIdentity(u.UserId, u.FullName, u.Email))
                .FirstOrDefaultAsync(cancellationToken);

            if (parentById != null)
            {
                return parentById;
            }
        }

        var email = user.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Email == email && u.RoleId == 2)
            .Select(u => new ParentIdentity(u.UserId, u.FullName, u.Email))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<ParentStudentSummary>> GetChildrenAsync(int parentId, CancellationToken cancellationToken = default)
    {
        return _context.ParentStudents
            .AsNoTracking()
            .Where(ps => ps.ParentId == parentId)
            .OrderBy(ps => ps.Student.FullName)
            .Select(ps => new ParentStudentSummary(
                ps.StudentId,
                ps.Student.FullName,
                ps.Relationship))
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUnreadNotificationCountAsync(int parentId, CancellationToken cancellationToken = default)
    {
        return _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == parentId && n.IsRead != true, cancellationToken);
    }
}

public sealed record ParentIdentity(int UserId, string FullName, string? Email);
public sealed record ParentStudentSummary(int StudentId, string StudentName, string? Relationship);
