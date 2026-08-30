using System.ComponentModel.DataAnnotations;

namespace smp_trask1.Models
{
    // Account fields shared by student and employee registration requests.
    public class UserRegisterDto
    {
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Password { get; set; } = null!;

        public int? RoleId { get; set; }
        public int? StatusId { get; set; }
    }

    public class UserUpdateDto
    {
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = null!;

        public int? RoleId { get; set; }
        public int? StatusId { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required, MaxLength(500)]
        public string NewPassword { get; set; } = null!;

        [Required, MaxLength(500)]
        public string OldPassword { get; set; } = null!;
    }

    // All employee operations use this request. Validation depends on Action.
    public class EmployeeManageDto
    {
        public int? Action { get; set; }
        public int? EmployeeId { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public int? RoleId { get; set; }
        public int? StatusId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int? CityId { get; set; }
        public string? Designation { get; set; }
        public DateOnly? JoiningDate { get; set; }
    }

    public class StudentManageRequest
    {
        public int? Action { get; set; }
        public int? StudentId { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public int? RoleId { get; set; }
        public int? StatusId { get; set; }
        public string? RegisterNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int? CityId { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? AcademicYear { get; set; }
        public List<StudentRegisterDocumentRequest> Documents { get; set; } = [];
    }

    public class StudentRegisterDocumentRequest
    {
        [Range(1, int.MaxValue)]
        public int StudentDocumentTypeId { get; set; }

        public string? FileUrl { get; set; }
    }

    public class StudentResetPasswordRequest
    {
        [Range(1, int.MaxValue)]
        public int StudentId { get; set; }

        [Required, MaxLength(500)]
        public string OldPassword { get; set; } = null!;

        [Required, MaxLength(500)]
        public string NewPassword { get; set; } = null!;
    }

    public class StudentAttendanceStudentListRequest
    {
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? AcademicYear { get; set; }
        public int Limit { get; set; } = 20;
        public int Offset { get; set; }
    }

    public class StudentAttendanceStudentDto
    {
        public int StudentId { get; set; }
        public string RegisterNumber { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
    }

    public class StudentAttendanceStudentListResponse
    {
        public int TotalCount { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public List<StudentAttendanceStudentDto> Students { get; set; } = [];
    }

    public class StudentAttendanceMarkRequest
    {
        public DateOnly? AttendanceDate { get; set; }
        public int MarkedByEmployeeId { get; set; }
        public List<StudentAttendanceMarkItemRequest> Students { get; set; } = [];
    }

    public class StudentAttendanceMarkItemRequest
    {
        public int StudentId { get; set; }
        public int AttendanceStatusId { get; set; }
        public string? Remarks { get; set; }
    }

    public class LocationRequest
    {
        public int? CountryId { get; set; }
        public int? StateId { get; set; }
    }

    public class LocationResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class EmployeeAttendanceRequest
    {
        [Required]
        [Range(1, 2)]
        public int Action { get; set; }

        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? AttendanceStatusId { get; set; }

        [MaxLength(250)]
        public string? Remarks { get; set; }
    }

    public class EmployeeAttendanceEditRequest
    {
        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }

        [Required]
        public DateOnly? AttendanceDate { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? AttendanceStatusId { get; set; }

        public TimeOnly? CheckInTime { get; set; }
        public TimeOnly? CheckOutTime { get; set; }

        [MaxLength(250)]
        public string? Remarks { get; set; }
    }
}
