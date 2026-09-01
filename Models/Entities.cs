using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smp_trask1.Models;

public class Status
{
    [Key]
    public int StatusId { get; set; }
    [Required, MaxLength(30)]
    public string StatusName { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public ICollection<User> Users { get; set; } = [];
}

public class Role
{
    [Key]
    public int RoleId { get; set; }
    [Required, MaxLength(30)]
    public string RoleName { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public ICollection<User> Users { get; set; } = [];
}

public class Country
{
    [Key]
    public int CountryId { get; set; }
    [Required, MaxLength(100)]
    public string CountryName { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public ICollection<State> States { get; set; } = [];
}

public class State
{
    [Key]
    public int StateId { get; set; }
    [Required, MaxLength(100)]
    public string StateName { get; set; } = null!;
    [ForeignKey(nameof(Country))]
    public int? CountryId { get; set; }
    public bool IsDeleted { get; set; }
    public Country? Country { get; set; }
    public ICollection<City> Cities { get; set; } = [];
}

public class City
{
    [Key]
    public int CityId { get; set; }
    [Required, MaxLength(100)]
    public string CityName { get; set; } = null!;
    [ForeignKey(nameof(State))]
    public int? StateId { get; set; }
    public bool IsDeleted { get; set; }
    public State? State { get; set; }
    public ICollection<Student> Students { get; set; } = [];
    public ICollection<Employee> Employees { get; set; } = [];
}

public class StudentDocumentType
{
    [Key]
    public int StudentDocumentTypeId { get; set; }
    [Required, MaxLength(50)]
    public string DocumentTypeName { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public ICollection<StudentDocument> StudentDocuments { get; set; } = [];
}

public class AttendanceStatus
{
    [Key]
    public int AttendanceStatusId { get; set; }
    [Required, MaxLength(20)]
    public string AttendanceStatusName { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public ICollection<StudentAttendance> StudentAttendances { get; set; } = [];
    public ICollection<EmployeeAttendance> EmployeeAttendances { get; set; } = [];
}

public class User
{
    [Key]
    public int UserId { get; set; }
    [Required, MaxLength(150)]
    public string Email { get; set; } = null!;
    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = null!;
    [ForeignKey(nameof(Role))]
    public int? RoleId { get; set; }
    [ForeignKey(nameof(Status))]
    public int? StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Role? Role { get; set; }
    public Status? Status { get; set; }
    public Student? Student { get; set; }
    public Employee? Employee { get; set; }
}

public class Student
{
    [Key]
    public int StudentId { get; set; }
    [ForeignKey(nameof(User))]
    public int? UserId { get; set; }
    [Required, MaxLength(30)]
    public string RegisterNumber { get; set; } = null!;
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = null!;
    [MaxLength(50)]
    public string? LastName { get; set; }
    [Column(TypeName = "date")]
    public DateOnly DateOfBirth { get; set; }
    [Required, MaxLength(15)]
    public string Gender { get; set; } = null!;
    [Required, MaxLength(15)]
    public string PhoneNumber { get; set; } = null!;
    [Required, MaxLength(250)]
    public string Address { get; set; } = null!;
    [ForeignKey(nameof(City))]
    public int? CityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public User? User { get; set; }
    public City? City { get; set; }
    public ICollection<StudentDocument> StudentDocuments { get; set; } = [];
    public ICollection<StudentAttendance> StudentAttendances { get; set; } = [];
    public ICollection<StudentEnrollment> StudentEnrollments { get; set; } = [];
}

public class StudentEnrollment
{
    [Key]
    public int StudentEnrollmentId { get; set; }

    [ForeignKey(nameof(Student))]
    public int StudentId { get; set; }

    [Required, MaxLength(30)]
    public string ClassName { get; set; } = null!;

    [Required, MaxLength(20)]
    public string SectionName { get; set; } = null!;

    [Required, MaxLength(20)]
    public string AcademicYear { get; set; } = null!;

    [Required, MaxLength(50)]
    public string GroupName { get; set; } = "General";

    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public Student? Student { get; set; }
    public ICollection<StudentMark> StudentMarks { get; set; } = [];
}

public class Subject
{
    [Key]
    public int SubjectId { get; set; }

    [Required, MaxLength(20)]
    public string SubjectCode { get; set; } = null!;

    [Required, MaxLength(100)]
    public string SubjectName { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal MaximumMarks { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MinimumPassMarks { get; set; }

    public bool IsDeleted { get; set; }
    public ICollection<ClassSubjectMapping> ClassSubjectMappings { get; set; } = [];
    public ICollection<StudentMark> StudentMarks { get; set; } = [];
}

public class ClassSubjectMapping
{
    [Key]
    public int ClassSubjectMappingId { get; set; }

    [Required, MaxLength(30)]
    public string ClassName { get; set; } = null!;

    [MaxLength(20)]
    public string? SectionName { get; set; }

    [Required, MaxLength(50)]
    public string GroupName { get; set; } = "General";

    [Required, MaxLength(20)]
    public string AcademicYear { get; set; } = null!;

    [ForeignKey(nameof(Subject))]
    public int SubjectId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Subject? Subject { get; set; }
}

public class Exam
{
    [Key]
    public int ExamId { get; set; }

    [Required, MaxLength(100)]
    public string ExamName { get; set; } = null!;

    [Required, MaxLength(30)]
    public string ClassName { get; set; } = null!;

    [MaxLength(20)]
    public string? SectionName { get; set; }

    [Required, MaxLength(50)]
    public string GroupName { get; set; } = "General";

    [Required, MaxLength(20)]
    public string AcademicYear { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateOnly ExamDate { get; set; }

    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<StudentMark> StudentMarks { get; set; } = [];
}

public class StudentMark
{
    [Key]
    public int StudentMarkId { get; set; }

    [ForeignKey(nameof(StudentEnrollment))]
    public int StudentEnrollmentId { get; set; }

    [ForeignKey(nameof(Exam))]
    public int ExamId { get; set; }

    [ForeignKey(nameof(Subject))]
    public int SubjectId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MarksObtained { get; set; }

    [ForeignKey(nameof(EnteredByEmployee))]
    public int EnteredByEmployeeId { get; set; }

    [MaxLength(250)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public StudentEnrollment? StudentEnrollment { get; set; }
    public Exam? Exam { get; set; }
    public Subject? Subject { get; set; }
    public Employee? EnteredByEmployee { get; set; }
}

public class Employee
{
    [Key]
    public int EmployeeId { get; set; }
    [ForeignKey(nameof(User))]
    public int? UserId { get; set; }
    [Required, MaxLength(30)]
    public string EmployeeCode { get; set; } = null!;
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = null!;
    [MaxLength(50)]
    public string? LastName { get; set; }
    [Column(TypeName = "date")]
    public DateOnly DateOfBirth { get; set; }
    [Required, MaxLength(15)]
    public string Gender { get; set; } = null!;
    [Required, MaxLength(15)]
    public string PhoneNumber { get; set; } = null!;
    [Required, MaxLength(250)]
    public string Address { get; set; } = null!;
    [ForeignKey(nameof(City))]
    public int? CityId { get; set; }
    [Required, MaxLength(100)]
    public string Designation { get; set; } = null!;
    [Column(TypeName = "date")]
    public DateOnly JoiningDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public User? User { get; set; }
    public City? City { get; set; }
    public ICollection<StudentAttendance> MarkedStudentAttendances { get; set; } = [];
    public ICollection<EmployeeAttendance> EmployeeAttendances { get; set; } = [];
    public ICollection<StudentMark> EnteredStudentMarks { get; set; } = [];
}

public class StudentDocument
{
    [Key]
    public int StudentDocumentId { get; set; }
    [ForeignKey(nameof(Student))]
    public int? StudentId { get; set; }
    [ForeignKey(nameof(StudentDocumentType))]
    public int? StudentDocumentTypeId { get; set; }
    [Required, MaxLength(150)]
    public string DocumentName { get; set; } = null!;
    [Required, MaxLength(500)]
    public string FileUrl { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Student? Student { get; set; }
    public StudentDocumentType? StudentDocumentType { get; set; }
}

public class StudentAttendance
{
    [Key]
    public int StudentAttendanceId { get; set; }
    [ForeignKey(nameof(Student))]
    public int? StudentId { get; set; }
    [Column(TypeName = "date")]
    public DateOnly AttendanceDate { get; set; }
    [ForeignKey(nameof(AttendanceStatus))]
    public int? AttendanceStatusId { get; set; }
    [ForeignKey(nameof(MarkedByEmployee))]
    public int? MarkedByEmployeeId { get; set; }
    [MaxLength(250)]
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Student? Student { get; set; }
    public AttendanceStatus? AttendanceStatus { get; set; }
    public Employee? MarkedByEmployee { get; set; }
}

public class EmployeeAttendance
{
    [Key]
    public int EmployeeAttendanceId { get; set; }
    [ForeignKey(nameof(Employee))]
    public int? EmployeeId { get; set; }
    [Column(TypeName = "date")]
    public DateOnly AttendanceDate { get; set; }
    [ForeignKey(nameof(AttendanceStatus))]
    public int? AttendanceStatusId { get; set; }
    [Column(TypeName = "time")]
    public TimeOnly? CheckInTime { get; set; }
    [Column(TypeName = "time")]
    public TimeOnly? CheckOutTime { get; set; }
    [MaxLength(250)]
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Employee? Employee { get; set; }
    public AttendanceStatus? AttendanceStatus { get; set; }
}
