using System.Security.Claims;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages;

public class LoginModel : PageModel
{
    private readonly ThuVienContext _context;

    public LoginModel(ThuVienContext context)
    {
        _context = context;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

        // Chuỗi kết nối Database của bạn
        private readonly string connectionString = @"Server=QUYNH_CHI\KTEAM;database=Thu_vien;uid=sa;pwd=123456;TrustServerCertificate=True;";

        public void OnGet()
        {
            // Chạy khi người dùng mới vào trang Login
        }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == Email && u.PasswordHash == Password);

        if (user == null)
        {
            ErrorMessage = "Email hoặc mật khẩu không đúng.";
            return Page();
        }

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Tài khoản của bạn đã bị khóa.";
            return Page();
        }

        var roleName = user.Role?.RoleName ?? GetRoleName(user.RoleId);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new("UserId", user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, roleName)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = true };

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", roleName);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

                                case "Librarian":
                                    return RedirectToPage("/LibrarianDashboard"); // Chuyển đến trang của Thủ thư

    private IActionResult RedirectByRole(string? roleName)
    {
        return roleName switch
        {
            "Admin" => RedirectToPage("/AdminDashboard"),
            "Librarian" => RedirectToPage("/Librarian/Libralian_Dashboard"),
            "Teacher" => RedirectToPage("/TeacherDashboard"),
            "Parent" => RedirectToPage("/Parent/Dashboard"),
            "Student" => RedirectToPage("/StudentDashboard"),
            _ => RedirectToPage("/Index")
        };
    }

    private static string GetRoleName(int roleId)
    {
        return roleId switch
        {
            5 => "Admin",
            4 => "Librarian",
            3 => "Teacher",
            2 => "Parent",
            1 => "Student",
            _ => "User"
        };
    }
}
