using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("UserId");

        return int.TryParse(rawUserId, out var userId) ? userId : null;
    }
}
