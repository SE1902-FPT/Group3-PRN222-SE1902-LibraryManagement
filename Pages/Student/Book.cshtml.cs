using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Student
{
    public class BorrowBooksModel : PageModel
    {
        private readonly ThuVienContext _context;

        public BorrowBooksModel(ThuVienContext context)
        {
            _context = context;
        }

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? MessageType { get; set; }

        public List<Book> Books { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public User? CurrentStudent { get; set; }
        public string? StudentClassName { get; set; }
        public int PendingCount { get; set; }
        public List<int> FavoriteBookIds { get; set; } = new();
        public Dictionary<int, string> BookRequestStatuses { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int? SelectedCategoryId { get; set; }
        public int TotalResults { get; set; }

        public async Task<IActionResult> OnGetAsync(int? categoryId, string? search)
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Student"))
            {
                return RedirectToPage("/Login", new { error = "access_denied" });
            }

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            CurrentStudent = await _context.Users
                .Include(u => u.ClassesNavigation)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (CurrentStudent == null)
                return RedirectToPage("/Login", new { error = "access_denied" });

            var studentId = CurrentStudent.UserId;
            StudentClassName = CurrentStudent.ClassesNavigation.FirstOrDefault()?.ClassName ?? "N/A";
            SearchTerm = search?.Trim() ?? string.Empty;
            SelectedCategoryId = categoryId;

            PendingCount = await _context.BorrowRequests
                .CountAsync(r => r.StudentId == studentId &&
                                 (r.Status == "Pending" || r.Status == "Approved" || r.Status == "Borrowed"));

            FavoriteBookIds = await _context.FavoriteBooks
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

            Categories = await _context.Categories.ToListAsync();

            var query = _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(b =>
                    b.Title.Contains(SearchTerm) ||
                    (b.Author != null && b.Author.Contains(SearchTerm)) ||
                    (b.Publisher != null && b.Publisher.Contains(SearchTerm)) ||
                    (b.Isbn != null && b.Isbn.Contains(SearchTerm)));
            }

            TotalResults = await query.CountAsync();
            Books = await query.OrderByDescending(b => b.BookId).ToListAsync();

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
