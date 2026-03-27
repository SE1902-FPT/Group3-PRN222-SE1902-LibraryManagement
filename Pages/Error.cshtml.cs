using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public string ErrorTitle { get; set; } = "Da xay ra loi";
        public string ErrorMessage { get; set; } = "Khong the xu ly yeu cau cua ban.";
        public string? ReturnUrl { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(string? error, string? returnUrl)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            ReturnUrl = returnUrl;

            if (string.Equals(error, "login_required", StringComparison.OrdinalIgnoreCase))
            {
                ErrorTitle = "Ban chua dang nhap";
                ErrorMessage = "Vui long dang nhap de tiep tuc truy cap chuc nang nay.";
                return;
            }

            if (string.Equals(error, "access_denied", StringComparison.OrdinalIgnoreCase))
            {
                ErrorTitle = "Khong co quyen truy cap";
                ErrorMessage = "Tai khoan cua ban khong du quyen de vao trang nay.";
            }
        }
    }

}
