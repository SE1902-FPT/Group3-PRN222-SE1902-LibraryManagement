using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        // Chuỗi kết nối Database của bạn
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;database=Thu_vien;uid=sa;pwd=123;TrustServerCertificate=True;";

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
            if (!ModelState.IsValid) return Page();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT UserID, FullName, RoleID, Status FROM [Thu_vien].[dbo].[Users] WHERE Email = @Email AND PasswordHash = @Password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@Password", Password);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string status = reader["Status"].ToString();
                            if (status != "Active")
                            {
                                ErrorMessage = "Tài khoản của bạn đã bị khóa!";
                                return Page();
                            }

                            string fullName = reader["FullName"].ToString();
                            int roleId = Convert.ToInt32(reader["RoleID"]);

                            // Chuyển RoleID thành Tên Quyền để dễ quản lý trong ASP.NET
                            string roleName = GetRoleName(roleId);

                            // 1. Tạo danh sách các thông tin (Claims) của người dùng
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, fullName),
                                new Claim(ClaimTypes.Email, Email),
                                new Claim(ClaimTypes.Role, roleName) // Đây là phần quan trọng nhất để phân quyền
                            };

                            // 2. Tạo Identity và Principal
                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties { IsPersistent = true }; // Ghi nhớ đăng nhập

                            // 3. Đăng nhập (Lưu Cookie)
                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);

                            // 4. Điều hướng sau khi đăng nhập thành công
                            switch (roleName)
                            {
                                case "Admin":
                                    return RedirectToPage("/AdminDashboard"); // Chuyển đến trang của Quản trị viên

                                case "Librarian":
                                    return RedirectToPage("/Librarian/Libralian_Dashboard"); // Chuyển đến trang của Thủ thư

                                case "Teacher":
                                    return RedirectToPage("/TeacherDashboard"); // Chuyển đến trang của Giáo viên

                                case "Parent":
                                    return RedirectToPage("/ParentDashboard"); // Chuyển đến trang của Phụ huynh

                                case "Student":
                                    return RedirectToPage("/index"); // Chuyển đến trang của Học sinh

                                default:
                                    // Đề phòng trường hợp lỗi hoặc có quyền lạ chưa được định nghĩa
                                    return RedirectToPage("/Index");
                            }
                        }
                        else
                        {
                            ErrorMessage = "Email hoặc mật khẩu không đúng!";
                            return Page();
                        }
                    }
                }
            }
        }

        // Hàm phụ trợ chuyển RoleID thành chuỗi
        private string GetRoleName(int roleId)
        {
            return roleId switch
            {
                5 => "Admin",
                4 => "Librarian", // Thủ thư
                3 => "Teacher",
                1 => "Student",
                2 => "Parent",
                _ => "User"
            };
        }
    }
}

