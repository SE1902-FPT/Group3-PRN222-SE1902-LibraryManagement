using Azure.Core;
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Books
{
    public class DetailsModel : PageModel
    {
        private readonly ThuVienContext _context;

        public DetailsModel(ThuVienContext context)
        {
            _context = context;
        }

        public Book? Book { get; set; }
        public List<TeacherRecommendation> TeacherRecommendations { get; set; } = new();

        public Dictionary<int, string> BookRequestStatuses { get; set; } = new();
        public int AvailableCopies { get; set; }

        public User? CurrentStudent { get; set; }
        public string? StudentClassName { get; set; }
        public int PendingCount { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsRequested { get; set; }
        public bool IsStudentView { get; set; }
        public bool IsTeacherView { get; set; }

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? MessageType { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (Book == null)
            {
                return NotFound();
            }

            AvailableCopies = Book.BookCopies.Count(c => c.Status == "Available");

            IsStudentView = User.Identity?.IsAuthenticated == true && User.IsInRole("Student");
            IsTeacherView = User.Identity?.IsAuthenticated == true && User.IsInRole("Teacher");

            if (IsStudentView)
            {
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

                IsRequested = await _context.BorrowRequests
                    .Include(r => r.Copy)
                    .AnyAsync(r => r.StudentId == studentId && r.Copy != null && r.Copy.BookId == id.Value &&
                                  (r.Status == "Pending" || r.Status == "Approved" || r.Status == "Borrowed"));

                // lấy trạng thái chi tiết của yêu cầu mượn sách này
                if (IsRequested)
                {
                    var activeRequests = await _context.BorrowRequests
                        .Include(r => r.Copy)
                        .Where(r => r.StudentId == studentId && r.Copy != null && r.Copy.BookId == id.Value &&
                                    (r.Status == "Pending" || r.Status == "Approved" || r.Status == "Borrowed"))
                        .ToListAsync();

                    // Ưu tiên: Borrowed > Approved > Pending
                    string resolvedStatus = activeRequests.Any(r => r.Status == "Borrowed") ? "Borrowed" :
                                           activeRequests.Any(r => r.Status == "Approved") ? "Approved" : "Pending";
                    BookRequestStatuses[id.Value] = resolvedStatus;
                }
            }

            TeacherRecommendations = await _context.TeacherRecommendations
                .Include(r => r.Teacher)
                .Where(r => r.BookId == id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int bookId)
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
            }
            else
            {
                _context.FavoriteBooks.Add(new FavoriteBook
                {
                    StudentId = student.UserId,
                    BookId = bookId,
                    CreatedAt = DateTime.Now
                });
                Message = "Đã thêm sách vào danh sách yêu thích";
            }

            MessageType = "success";
            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = bookId });
        }

        public async Task<IActionResult> OnPostBorrowAsync(int copyId, int bookId, DateTime expectedReturnDate)
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
                return RedirectToPage(new { id = bookId });
            }

            var alreadyRequested = await _context.BorrowRequests
                .Include(r => r.Copy)
                .AnyAsync(r => r.StudentId == studentId &&
                               r.Copy != null &&
                               r.Copy.BookId == bookId &&
                               (r.Status == "Pending" || r.Status == "Approved" || r.Status == "Borrowed"));

            if (alreadyRequested)
            {
                Message = "Bạn đã đăng ký mượn cuốn sách này rồi!";
                MessageType = "error";
                return RedirectToPage(new { id = bookId });
            }

            var pendingCount = await _context.BorrowRequests
                .CountAsync(r => r.StudentId == studentId && r.Status == "Pending");

            if (pendingCount >= 5)
            {
                Message = "Bạn chỉ được mượn tối đa 5 quyển sách cùng lúc!";
                MessageType = "error";
                return RedirectToPage(new { id = bookId });
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

            Message = $"Đăng ký mượn cuốn \"{copy.Book.Title}\" thành công! Ngày dự kiến trả: {expectedReturnDate:dd/MM/yyyy}";
            MessageType = "success";
            return RedirectToPage(new { id = bookId });
        }
    }
}
