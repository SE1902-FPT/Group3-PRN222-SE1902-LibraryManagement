using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<BorrowRecord> BorrowRecordProcessedByNavigations { get; set; } = new List<BorrowRecord>();

    public virtual ICollection<BorrowRecord> BorrowRecordStudents { get; set; } = new List<BorrowRecord>();

    public virtual ICollection<BorrowRequest> BorrowRequestApprovedByNavigations { get; set; } = new List<BorrowRequest>();

    public virtual ICollection<BorrowRequest> BorrowRequestStudents { get; set; } = new List<BorrowRequest>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<ParentStudent> ParentStudentParents { get; set; } = new List<ParentStudent>();

    public virtual ICollection<ParentStudent> ParentStudentStudents { get; set; } = new List<ParentStudent>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<TeacherRecommendation> TeacherRecommendations { get; set; } = new List<TeacherRecommendation>();

    public virtual ICollection<Class> ClassesNavigation { get; set; } = new List<Class>();
}
