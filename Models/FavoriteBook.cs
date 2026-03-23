using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class FavoriteBook
{
    public int FavoriteId { get; set; }

    public int StudentId { get; set; }

    public int BookId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
