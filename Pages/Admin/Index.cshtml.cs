using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Group3_SE1902_PRN222_LibraryManagement.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly string connectionString = @"Server=localhost;database=Thu_vien;uid=sa;pwd=123;TrustServerCertificate=True;";

        public int TotalBooks { get; set; }
        public int BooksBorrowed { get; set; }
        public int TotalUsers { get; set; }
        public int PendingRequests { get; set; }

        public List<BookInfo> Books { get; set; } = new();
        public List<StatsBook> TopFavoriteBooks { get; set; } = new();
        public List<StatsBook> TopBorrowedBooks { get; set; } = new();

        public void OnGet()
        {
            GetStatistics();
            GetBooksList();
            GetTopFavoriteBooks();
            GetTopBorrowedBooks();
        }

        private void GetStatistics()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            // Total Books
            using (SqlCommand cmd = new SqlCommand("SELECT SUM(TotalQuantity) FROM Books", conn))
            {
                var result = cmd.ExecuteScalar();
                TotalBooks = result != DBNull.Value && result != null ? Convert.ToInt32(result) : 0;
            }

            // Books Borrowed
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM BorrowRecords WHERE ReturnDate IS NULL", conn))
            {
                BooksBorrowed = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Total Users
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
            {
                TotalUsers = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Pending Requests
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM BorrowRequests WHERE Status = 'Pending'", conn))
            {
                PendingRequests = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void GetBooksList()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            string query = @"SELECT b.BookID, b.Title, b.Author, b.TotalQuantity, c.CategoryName,
                           (SELECT COUNT(*) FROM BorrowRecords br 
                            JOIN BookCopies bc ON br.CopyID = bc.CopyID 
                            WHERE bc.BookID = b.BookID AND br.ReturnDate IS NULL) as BorrowedCount
                           FROM Books b
                           LEFT JOIN Categories c ON b.CategoryID = c.CategoryID";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Books.Add(new BookInfo
                {
                    BookID = Convert.ToInt32(reader["BookID"]),
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"].ToString(),
                    CategoryName = reader["CategoryName"] != DBNull.Value ? reader["CategoryName"].ToString() : "N/A",
                    TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                    BorrowedCount = Convert.ToInt32(reader["BorrowedCount"])
                });
            }
        }

        private void GetTopFavoriteBooks()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            string query = @"SELECT ROW_NUMBER() OVER (ORDER BY FavoriteCount DESC) as RowNum, Title, FavoriteCount
                           FROM (
                               SELECT b.Title, COUNT(fb.FavoriteID) as FavoriteCount
                               FROM FavoriteBooks fb
                               JOIN Books b ON fb.BookID = b.BookID
                               GROUP BY b.BookID, b.Title
                           ) t
                           ORDER BY FavoriteCount DESC";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                TopFavoriteBooks.Add(new StatsBook
                {
                    RowNum = Convert.ToInt32(reader["RowNum"]),
                    Title = reader["Title"].ToString(),
                    FavoriteCount = Convert.ToInt32(reader["FavoriteCount"])
                });
            }
        }

        private void GetTopBorrowedBooks()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            string query = @"SELECT ROW_NUMBER() OVER (ORDER BY BorrowCount DESC) as RowNum, Title, BorrowCount
                           FROM (
                               SELECT b.Title, COUNT(br.BorrowID) as BorrowCount
                               FROM BorrowRecords br
                               JOIN BookCopies bc ON br.CopyID = bc.CopyID
                               JOIN Books b ON bc.BookID = b.BookID
                               GROUP BY b.BookID, b.Title
                           ) t
                           ORDER BY BorrowCount DESC";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                TopBorrowedBooks.Add(new StatsBook
                {
                    RowNum = Convert.ToInt32(reader["RowNum"]),
                    Title = reader["Title"].ToString(),
                    BorrowCount = Convert.ToInt32(reader["BorrowCount"])
                });
            }
        }
    }

    public class BookInfo
    {
        public int BookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string CategoryName { get; set; }
        public int TotalQuantity { get; set; }
        public int BorrowedCount { get; set; }
    }

    public class StatsBook
    {
        public int RowNum { get; set; }
        public string Title { get; set; }
        public int FavoriteCount { get; set; }
        public int BorrowCount { get; set; }
    }
}
