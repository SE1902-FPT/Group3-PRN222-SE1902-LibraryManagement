using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class TeacherRecommendation
{
    public int RecommendationId { get; set; }

    public int? TeacherId { get; set; }

    public int? BookId { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Book? Book { get; set; }

    public virtual User? Teacher { get; set; }
}
