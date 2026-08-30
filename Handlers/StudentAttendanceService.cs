using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers
{
    public class StudentAttendanceService
    {
        private readonly SmpDbContext _dbContext;

        public StudentAttendanceService(SmpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StudentAttendanceStudentListResponse> GetStudentsAsync(
            StudentAttendanceStudentListRequest request)
        {
            ValidateRequest(request);

            var className = request.ClassName!.Trim();
            var sectionName = request.SectionName!.Trim();
            var academicYear = request.AcademicYear!.Trim();

            var query = _dbContext.StudentEnrollments
                .AsNoTracking()
                .Where(enrollment =>
                    !enrollment.IsDeleted &&
                    enrollment.ClassName == className &&
                    enrollment.SectionName == sectionName &&
                    enrollment.AcademicYear == academicYear &&
                    enrollment.Student != null &&
                    !enrollment.Student.IsDeleted);

            var totalCount = await query.CountAsync();

            var students = await query
                .OrderBy(enrollment => enrollment.Student!.RegisterNumber)
                .Skip(request.Offset)
                .Take(request.Limit)
                .Select(enrollment => new StudentAttendanceStudentDto
                {
                    StudentId = enrollment.StudentId,
                    RegisterNumber = enrollment.Student!.RegisterNumber,
                    FirstName = enrollment.Student.FirstName,
                    LastName = enrollment.Student.LastName
                })
                .ToListAsync();

            return new StudentAttendanceStudentListResponse
            {
                TotalCount = totalCount,
                Limit = request.Limit,
                Offset = request.Offset,
                Students = students
            };
        }

        public async Task MarkAttendanceAsync(StudentAttendanceMarkRequest request)
        {
            ValidateMarkRequest(request);

            var attendanceDate = request.AttendanceDate!.Value;
            var studentIds = request.Students.Select(item => item.StudentId).ToList();
            var statusIds = request.Students
                .Select(item => item.AttendanceStatusId)
                .Distinct()
                .ToList();

            var employeeExists = await _dbContext.Employees.AnyAsync(employee =>
                employee.EmployeeId == request.MarkedByEmployeeId && !employee.IsDeleted);

            if (!employeeExists)
                throw new KeyNotFoundException("Marking employee not found.");

            var activeStudentCount = await _dbContext.Students.CountAsync(student =>
                studentIds.Contains(student.StudentId) && !student.IsDeleted);

            if (activeStudentCount != studentIds.Count)
                throw new KeyNotFoundException("One or more students were not found.");

            var activeStatusCount = await _dbContext.AttendanceStatuses.CountAsync(status =>
                statusIds.Contains(status.AttendanceStatusId) && !status.IsDeleted);

            if (activeStatusCount != statusIds.Count)
                throw new KeyNotFoundException("One or more attendance statuses were not found.");

            var attendanceExists = await _dbContext.StudentAttendances.AnyAsync(attendance =>
                studentIds.Contains(attendance.StudentId ?? 0) &&
                attendance.AttendanceDate == attendanceDate);

            if (attendanceExists)
                throw new InvalidOperationException(
                    "Attendance has already been marked for one or more students on this date.");

            var now = DateTime.UtcNow;
            var attendanceRecords = request.Students.Select(item => new StudentAttendance
            {
                StudentId = item.StudentId,
                AttendanceDate = attendanceDate,
                AttendanceStatusId = item.AttendanceStatusId,
                MarkedByEmployeeId = request.MarkedByEmployeeId,
                Remarks = NormalizeRemarks(item.Remarks),
                CreatedAt = now
            });

            await _dbContext.StudentAttendances.AddRangeAsync(attendanceRecords);
            await _dbContext.SaveChangesAsync();
        }

        private static void ValidateRequest(StudentAttendanceStudentListRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClassName))
                throw new ArgumentException("ClassName is required.");

            if (string.IsNullOrWhiteSpace(request.SectionName))
                throw new ArgumentException("SectionName is required.");

            if (string.IsNullOrWhiteSpace(request.AcademicYear))
                throw new ArgumentException("AcademicYear is required.");

            if (request.Limit < 1 || request.Limit > 100)
                throw new ArgumentException("Limit must be between 1 and 100.");

            if (request.Offset < 0)
                throw new ArgumentException("Offset cannot be negative.");
        }

        private static void ValidateMarkRequest(StudentAttendanceMarkRequest request)
        {
            if (!request.AttendanceDate.HasValue)
                throw new ArgumentException("AttendanceDate is required.");

            if (request.MarkedByEmployeeId <= 0)
                throw new ArgumentException("MarkedByEmployeeId is required.");

            if (request.Students.Count == 0)
                throw new ArgumentException("At least one student is required.");

            if (request.Students.Any(item => item.StudentId <= 0))
                throw new ArgumentException("Every StudentId must be valid.");

            if (request.Students.Any(item => item.AttendanceStatusId <= 0))
                throw new ArgumentException("Every AttendanceStatusId must be valid.");

            if (request.Students.Any(item => item.Remarks?.Length > 250))
                throw new ArgumentException("Remarks cannot exceed 250 characters.");

            if (request.Students.Select(item => item.StudentId).Distinct().Count() !=
                request.Students.Count)
                throw new ArgumentException("The same student cannot appear more than once.");
        }

        private static string? NormalizeRemarks(string? remarks) =>
            string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }
}
