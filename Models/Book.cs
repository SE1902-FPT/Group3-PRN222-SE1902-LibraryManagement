using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class Book
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public string? Author { get; set; }

    public string? Isbn { get; set; }

    public int? CategoryId { get; set; }

    public string? Publisher { get; set; }

    public int? PublishYear { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public int? TotalQuantity { get; set; }

    public virtual ICollection<BookCopy> BookCopies { get; set; } = new List<BookCopy>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<FavoriteBook> FavoriteBooks { get; set; } = new List<FavoriteBook>();

    public virtual ICollection<TeacherRecommendation> TeacherRecommendations { get; set; } = new List<TeacherRecommendation>();
}
