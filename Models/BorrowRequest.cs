using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class BorrowRequest
{
    public int RequestId { get; set; }

    public int? StudentId { get; set; }

    public int? CopyId { get; set; }

    public DateTime? RequestDate { get; set; }

    public string? Status { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ExpectedReturnDate { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual BookCopy? Copy { get; set; }

    public virtual User? Student { get; set; }
}
