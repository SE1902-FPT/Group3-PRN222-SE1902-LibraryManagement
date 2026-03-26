using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages
{
    [Authorize(Roles = "Parent")]
    public class ParentDashboardModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect from old login path to the new organized Parent module
            return RedirectToPage("/Parent/Dashboard");
        }
    }
}
