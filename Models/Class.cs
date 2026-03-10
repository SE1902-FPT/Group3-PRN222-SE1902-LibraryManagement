using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public string? ClassName { get; set; }

    public int? TeacherId { get; set; }

    public virtual User? Teacher { get; set; }

    public virtual ICollection<User> Students { get; set; } = new List<User>();
}
