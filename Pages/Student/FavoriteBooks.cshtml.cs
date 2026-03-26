using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Student
{
    public class FavoriteBooksModel : PageModel
    {
        private readonly ThuVienContext _context;

        public FavoriteBooksModel(ThuVienContext context)
        {
            _context = context;
        }

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? MessageType { get; set; }

        public List<Book> Books { get; set; } = new();
        public User? CurrentStudent { get; set; }
        public string? StudentClassName { get; set; }
        public int PendingCount { get; set; }
        public Dictionary<int, string> BookRequestStatuses { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Student"))
            {
                return NotFound();
            }

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            CurrentStudent = await _context.Users
                .Include(u => u.ClassesNavigation)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (CurrentStudent == null)
            {
                return NotFound();
            }

            var studentId = CurrentStudent.UserId;
            StudentClassName = CurrentStudent.ClassesNavigation.FirstOrDefault()?.ClassName ?? "N/A";

            PendingCount = await _context.BorrowRequests
                .CountAsync(r => r.StudentId == studentId &&
                                 (r.Status == "Pending" || r.Status == "Approved" || r.Status == "Borrowed"));

            var favoriteBookIds = await _context.FavoriteBooks
                .Where(fb => fb.StudentId == studentId)
                .Select(fb => fb.BookId)
                .ToListAsync();

            var requests = await _context.BorrowRequests
                .Include(r => r.Copy)
                .Where(r => r.StudentId == studentId &&
                            r.Copy != null &&
                            r.Copy.BookId != null &&
                            (r.Status == "Pending" || r.Status == "Approved" || r.Status == "Borrowed"))
                .ToListAsync();

            BookRequestStatuses = requests
                .GroupBy(r => r.Copy!.BookId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Any(r => r.Status == "Borrowed") ? "Borrowed" :
                         g.Any(r => r.Status == "Approved") ? "Approved" : "Pending");

            Books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                .Where(b => favoriteBookIds.Contains(b.BookId))
                .OrderByDescending(b => b.BookId)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFavoriteAsync(int bookId)
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Student"))
            {
                return NotFound();
            }

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (student == null)
            {
                return NotFound();
            }

            var favorite = await _context.FavoriteBooks
                .FirstOrDefaultAsync(fb => fb.StudentId == student.UserId && fb.BookId == bookId);

            if (favorite != null)
            {
                _context.FavoriteBooks.Remove(favorite);
                Message = "Đã bỏ sách khỏi danh sách yêu thích.";
                MessageType = "success";
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBorrowAsync(int copyId, DateTime expectedReturnDate)
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Student"))
            {
                return NotFound();
            }

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (student == null)
            {
                return NotFound();
            }

            var studentId = student.UserId;

            var copy = await _context.BookCopies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.CopyId == copyId);

            if (copy == null || copy.Status != "Available")
            {
                Message = "Sách này hiện không còn sẵn để mượn!";
                MessageType = "error";
                return RedirectToPage();
            }

            var alreadyRequestedThisCopy = await _context.BorrowRequests
                .AnyAsync(r => r.StudentId == studentId &&
                               r.CopyId == copyId &&
                               r.Status == "Pending");

            if (alreadyRequestedThisCopy)
            {
                Message = "Bạn đã gửi yêu cầu mượn quyển sách này rồi, hãy chờ thủ thư duyệt!";
                MessageType = "error";
                return RedirectToPage();
            }

            var pendingCount = await _context.BorrowRequests
                .CountAsync(r => r.StudentId == studentId && r.Status == "Pending");

            if (pendingCount >= 5)
            {
                Message = "Bạn chỉ được tạo tối đa 5 yêu cầu!";
                MessageType = "error";
                return RedirectToPage();
            }

            var request = new BorrowRequest
            {
                StudentId = studentId,
                CopyId = copyId,
                RequestDate = DateTime.Now,
                Status = "Pending",
                ExpectedReturnDate = expectedReturnDate
            };

            _context.BorrowRequests.Add(request);
            await _context.SaveChangesAsync();

            var bookTitle = copy.Book?.Title ?? "sách";
            Message = $"Đăng ký mượn cuốn \"{bookTitle}\" thành công! Ngày dự kiến trả: {expectedReturnDate:dd/MM/yyyy}";
            MessageType = "success";
            return RedirectToPage();
        }
    }
}
