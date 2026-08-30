using Microsoft.EntityFrameworkCore;
using smp_trask1.Models;

namespace smp_trask1.Data;

public class SmpDbContext(DbContextOptions<SmpDbContext> options) : DbContext(options)
{
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<State> States { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<StudentDocumentType> StudentDocumentTypes { get; set; }
    public DbSet<AttendanceStatus> AttendanceStatuses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<StudentEnrollment> StudentEnrollments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<StudentDocument> StudentDocuments { get; set; }
    public DbSet<StudentAttendance> StudentAttendances { get; set; }
    public DbSet<EmployeeAttendance> EmployeeAttendances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasIndex(x => x.StatusName).IsUnique();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(x => x.RoleName).IsUnique();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasIndex(x => x.CountryName).IsUnique();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasIndex(x => new { x.CountryId, x.StateName }).IsUnique();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.Country).WithMany(x => x.States)
                .HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasIndex(x => new { x.StateId, x.CityName }).IsUnique();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.State).WithMany(x => x.Cities)
                .HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudentDocumentType>(entity =>
        {
            entity.HasIndex(x => x.DocumentTypeName).IsUnique();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<AttendanceStatus>(entity =>
        {
            entity.HasIndex(x => x.AttendanceStatusName).IsUnique();
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.Role).WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Status).WithMany(x => x.Users)
                .HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.RegisterNumber).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.User).WithOne(x => x.Student)
                .HasForeignKey<Student>(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.City).WithMany(x => x.Students)
                .HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudentEnrollment>(entity =>
        {
            entity.HasIndex(x => new { x.StudentId, x.AcademicYear }).IsUnique();
            entity.HasIndex(x => x.StudentId)
                .IsUnique()
                .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.IsCurrent).HasDefaultValue(false);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.Student).WithMany(x => x.StudentEnrollments)
                .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.EmployeeCode).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.User).WithOne(x => x.Employee)
                .HasForeignKey<Employee>(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.City).WithMany(x => x.Employees)
                .HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudentDocument>(entity =>
        {
            entity.Property(x => x.UploadedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => new { x.StudentId, x.StudentDocumentTypeId }).IsUnique();
            entity.HasOne(x => x.Student).WithMany(x => x.StudentDocuments)
                .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.StudentDocumentType).WithMany(x => x.StudentDocuments)
                .HasForeignKey(x => x.StudentDocumentTypeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudentAttendance>(entity =>
        {
            entity.HasIndex(x => new { x.StudentId, x.AttendanceDate }).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.Student).WithMany(x => x.StudentAttendances)
                .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.AttendanceStatus).WithMany(x => x.StudentAttendances)
                .HasForeignKey(x => x.AttendanceStatusId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.MarkedByEmployee).WithMany(x => x.MarkedStudentAttendances)
                .HasForeignKey(x => x.MarkedByEmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmployeeAttendance>(entity =>
        {
            entity.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique();
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_EmployeeAttendance_Time",
                    "[CheckOutTime] IS NULL OR [CheckInTime] IS NULL OR [CheckOutTime] >= [CheckInTime]");
            });
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasOne(x => x.Employee).WithMany(x => x.EmployeeAttendances)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.AttendanceStatus).WithMany(x => x.EmployeeAttendances)
                .HasForeignKey(x => x.AttendanceStatusId).OnDelete(DeleteBehavior.SetNull);
        });
    }

}
