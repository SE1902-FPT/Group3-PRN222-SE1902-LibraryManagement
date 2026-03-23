using System;
using System.Collections.Generic;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class VwParentNotification
{
    public int NotificationId { get; set; }

    public int? UserId { get; set; }

    public string? Message { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string ParentName { get; set; } = null!;
}
