using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            var error = Request.Query["error"].ToString();
            if (string.Equals(error, "login_required", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Bạn phải đăng nhập để tiếp tục.";
            }
            else if (string.Equals(error, "access_denied", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Bạn không có quyền truy cập chức năng này.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var connectionString = _configuration.GetConnectionString("ThuvienDB");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ErrorMessage = "Chưa cấu hình chuỗi kết nối cơ sở dữ liệu.";
                return Page();
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                const string query = "SELECT UserID, FullName, RoleID, Status FROM [Thu_vien].[dbo].[Users] WHERE Email = @Email AND PasswordHash = @Password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@Password", Password);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var status = reader["Status"]?.ToString();
                            if (status != "Active")
                            {
                                ErrorMessage = "Tài khoản của bạn đã bị khóa!";
                                return Page();
                            }

                            var fullName = reader["FullName"]?.ToString() ?? string.Empty;
                            int roleId = Convert.ToInt32(reader["RoleID"]);
                            string roleName = GetRoleName(roleId);

                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, fullName),
                                new Claim(ClaimTypes.Email, Email),
                                new Claim(ClaimTypes.Role, roleName)
                            };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties { IsPersistent = true };

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);

                            switch (roleName)
                            {
                                case "Admin":
                                    return RedirectToPage("/AdminDashboard");
                                case "Librarian":
                                    return RedirectToPage("/Librarian/Libralian_Dashboard");
                                case "Teacher":
                                    return RedirectToPage("/TeacherDashboard");
                                case "Parent":
                                    return RedirectToPage("/ParentDashboard");
                                case "Student":
                                    return RedirectToPage("/StudentDashboard");
                                default:
                                    return RedirectToPage("/Index");
                            }
                        }

                        ErrorMessage = "Email hoặc mật khẩu không đúng!";
                        return Page();
                    }
                }
            }
        }

        private string GetRoleName(int roleId)
        {
            return roleId switch
            {
                5 => "Admin",
                4 => "Librarian",
                3 => "Teacher",
                1 => "Student",
                2 => "Parent",
                _ => "User"
            };
        }
    }
}
