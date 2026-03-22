using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class ParentStudent
{
    public int ParentId { get; set; }

    public int StudentId { get; set; }

    public string? Relationship { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public virtual User Parent { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
