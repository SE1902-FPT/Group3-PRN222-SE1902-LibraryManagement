using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class BorrowRecord
{
    public int BorrowId { get; set; }

    public int? StudentId { get; set; }

    public int? CopyId { get; set; }

    public DateTime? BorrowDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string? Status { get; set; }

    public int? ProcessedBy { get; set; }

    public virtual BookCopy? Copy { get; set; }

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual User? Student { get; set; }
}
