using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement.Models;

public partial class ThuVienContext : DbContext
{
    public ThuVienContext()
    {
    }

    public ThuVienContext(DbContextOptions<ThuVienContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<BookCopy> BookCopies { get; set; }

    public virtual DbSet<BorrowRecord> BorrowRecords { get; set; }

    public virtual DbSet<BorrowRequest> BorrowRequests { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<FavoriteBook> FavoriteBooks { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<ParentStudent> ParentStudents { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TeacherRecommendation> TeacherRecommendations { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwParentBorrowInfo> VwParentBorrowInfos { get; set; }

    public virtual DbSet<VwParentNotification> VwParentNotifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:ThuvienDB");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PK__Books__3DE0C2273C99C93B");

            entity.HasIndex(e => e.Isbn, "UQ__Books__447D36EAA51E9F1F").IsUnique();

            entity.Property(e => e.BookId).HasColumnName("BookID");
            entity.Property(e => e.Author).HasMaxLength(100);
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("ImageURL");
            entity.Property(e => e.Isbn)
                .HasMaxLength(20)
                .HasColumnName("ISBN");
            entity.Property(e => e.Publisher).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.Books)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Books__CategoryI__4CA06362");
        });

        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.HasKey(e => e.CopyId).HasName("PK__BookCopi__C26CCCE5503DEE2B");

            entity.Property(e => e.CopyId).HasColumnName("CopyID");
            entity.Property(e => e.BookId).HasColumnName("BookID");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Available");

            entity.HasOne(d => d.Book).WithMany(p => p.BookCopies)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__BookCopie__BookI__5070F446");
        });

        modelBuilder.Entity<BorrowRecord>(entity =>
        {
            entity.HasKey(e => e.BorrowId).HasName("PK__BorrowRe__4295F85F9A155FD1");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("trg_BorrowRecords_Insert");
                    tb.HasTrigger("trg_BorrowRecords_Update");
                });

            entity.HasIndex(e => new { e.DueDate, e.ReturnDate, e.IsNotifiedOverdue }, "IX_BorrowRecords_OverdueCheck").HasFilter("([ReturnDate] IS NULL)");

            entity.Property(e => e.BorrowId).HasColumnName("BorrowID");
            entity.Property(e => e.BorrowDate).HasColumnType("datetime");
            entity.Property(e => e.CopyId).HasColumnName("CopyID");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.IsNotifiedOverdue).HasDefaultValue(false);
            entity.Property(e => e.ReturnDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Copy).WithMany(p => p.BorrowRecords)
                .HasForeignKey(d => d.CopyId)
                .HasConstraintName("FK__BorrowRec__CopyI__5BE2A6F2");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.BorrowRecordProcessedByNavigations)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("FK__BorrowRec__Proce__5CD6CB2B");

            entity.HasOne(d => d.Student).WithMany(p => p.BorrowRecordStudents)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__BorrowRec__Stude__5AEE82B9");
        });

        modelBuilder.Entity<BorrowRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__BorrowRe__33A8519ACDC7C496");

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.CopyId).HasColumnName("CopyID");
            entity.Property(e => e.ExpectedReturnDate).HasColumnType("datetime");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.BorrowRequestApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK__BorrowReq__Appro__571DF1D5");

            entity.HasOne(d => d.Copy).WithMany(p => p.BorrowRequests)
                .HasForeignKey(d => d.CopyId)
                .HasConstraintName("FK__BorrowReq__CopyI__5629CD9C");

            entity.HasOne(d => d.Student).WithMany(p => p.BorrowRequestStudents)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__BorrowReq__Stude__5535A963");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2B477DF560");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927A0B200D083");

            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.ClassName).HasMaxLength(50);
            entity.Property(e => e.TeacherId).HasColumnName("TeacherID");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__Classes__Teacher__4316F928");
        });

        modelBuilder.Entity<FavoriteBook>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__CE74FAF53471C1F7");

            entity.HasIndex(e => new { e.StudentId, e.BookId }, "UQ_FavoriteBooks_Student_Book").IsUnique();

            entity.Property(e => e.FavoriteId).HasColumnName("FavoriteID");
            entity.Property(e => e.BookId).HasColumnName("BookID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Book).WithMany(p => p.FavoriteBooks)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FavoriteBooks_Book");

            entity.HasOne(d => d.Student).WithMany(p => p.FavoriteBooks)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FavoriteBooks_User");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E32768AF218");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__619B8048");
        });

        modelBuilder.Entity<ParentStudent>(entity =>
        {
            entity.HasKey(e => new { e.ParentId, e.StudentId }).HasName("PK__ParentSt__501503A854726988");

            entity.ToTable("ParentStudent");

            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Relationship).HasMaxLength(50);

            entity.HasOne(d => d.Parent).WithMany(p => p.ParentStudentParents)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ParentStu__Paren__3F466844");

            entity.HasOne(d => d.Student).WithMany(p => p.ParentStudentStudents)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ParentStu__Stude__403A8C7D");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AB33E9A15");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<TeacherRecommendation>(entity =>
        {
            entity.HasKey(e => e.RecommendationId).HasName("PK__TeacherR__AA15BEC42CDABA99");

            entity.Property(e => e.RecommendationId).HasColumnName("RecommendationID");
            entity.Property(e => e.BookId).HasColumnName("BookID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.TeacherId).HasColumnName("TeacherID");

            entity.HasOne(d => d.Book).WithMany(p => p.TeacherRecommendations)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__TeacherRe__BookI__66603565");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherRecommendations)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__TeacherRe__Teach__656C112C");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC3F655F9F");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534CBBB008E").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleID__3C69FB99");

            entity.HasMany(d => d.ClassesNavigation).WithMany(p => p.Students)
                .UsingEntity<Dictionary<string, object>>(
                    "StudentClass",
                    r => r.HasOne<Class>().WithMany()
                        .HasForeignKey("ClassId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__StudentCl__Class__46E78A0C"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__StudentCl__Stude__45F365D3"),
                    j =>
                    {
                        j.HasKey("StudentId", "ClassId").HasName("PK__StudentC__2E74B803464EA14A");
                        j.ToTable("StudentClass");
                        j.IndexerProperty<int>("StudentId").HasColumnName("StudentID");
                        j.IndexerProperty<int>("ClassId").HasColumnName("ClassID");
                    });
        });

        modelBuilder.Entity<VwParentBorrowInfo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ParentBorrowInfo");

            entity.Property(e => e.BookTitle).HasMaxLength(200);
            entity.Property(e => e.BorrowDate).HasColumnType("datetime");
            entity.Property(e => e.BorrowId).HasColumnName("BorrowID");
            entity.Property(e => e.BorrowStatus).HasMaxLength(9);
            entity.Property(e => e.CopyId).HasColumnName("CopyID");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.ReturnDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.StudentName).HasMaxLength(100);
        });

        modelBuilder.Entity<VwParentNotification>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ParentNotifications");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.ParentName).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
