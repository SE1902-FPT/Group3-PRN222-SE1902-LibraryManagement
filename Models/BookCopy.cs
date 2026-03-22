using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class BookCopy
{
    public int CopyId { get; set; }

    public int? BookId { get; set; }

    public string? Status { get; set; }

    public virtual Book? Book { get; set; }

    public virtual ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();

    public virtual ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
}
