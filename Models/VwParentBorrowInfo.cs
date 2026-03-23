using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class VwParentBorrowInfo
{
    public int ParentId { get; set; }

    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public int? BorrowId { get; set; }

    public DateTime? BorrowDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string? Status { get; set; }

    public string? BookTitle { get; set; }

    public int? CopyId { get; set; }

    public string BorrowStatus { get; set; } = null!;
}
