using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers;

public class StudentMarksService
{
    private readonly SmpDbContext dbContext;

    public StudentMarksService(SmpDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<StudentMarksFetchResponse> FetchAsync(StudentMarksFetchRequest request)
    {
        ValidateSelection(request.ClassName, request.SectionName, request.GroupName,
            request.AcademicYear, request.ExamId, request.Limit, request.Offset);
        var className = request.ClassName!.Trim();
        var sectionName = request.SectionName!.Trim();
        var groupName = request.GroupName!.Trim();
        var academicYear = request.AcademicYear!.Trim();
        var exam = await GetMatchingExamAsync(request.ExamId, className, sectionName, groupName, academicYear);

        var mappedSubjects = await dbContext.ClassSubjectMappings.AsNoTracking()
            .Where(mapping => !mapping.IsDeleted && mapping.ClassName == className &&
                mapping.GroupName == groupName && mapping.AcademicYear == academicYear &&
                (mapping.SectionName == null || mapping.SectionName == sectionName) &&
                mapping.Subject != null && !mapping.Subject.IsDeleted)
            .Select(mapping => new StudentMarksSubjectDto
            {
                SubjectId = mapping.SubjectId,
                SubjectCode = mapping.Subject!.SubjectCode,
                SubjectName = mapping.Subject.SubjectName,
                MaximumMarks = mapping.Subject.MaximumMarks,
                MinimumPassMarks = mapping.Subject.MinimumPassMarks
            }).ToListAsync();
        var subjects = mappedSubjects.GroupBy(subject => subject.SubjectId)
            .Select(group => group.First()).OrderBy(subject => subject.SubjectName).ToList();
        if (subjects.Count == 0) throw new KeyNotFoundException("No subjects are mapped for the selected class.");

        var enrollments = dbContext.StudentEnrollments.AsNoTracking()
            .Where(enrollment => !enrollment.IsDeleted && enrollment.ClassName == className &&
                enrollment.SectionName == sectionName && enrollment.GroupName == groupName &&
                enrollment.AcademicYear == academicYear && enrollment.Student != null &&
                !enrollment.Student.IsDeleted);
        var totalCount = await enrollments.CountAsync();
        var students = await enrollments.OrderBy(enrollment => enrollment.Student!.RegisterNumber)
            .Skip(request.Offset).Take(request.Limit)
            .Select(enrollment => new StudentMarksStudentDto
            {
                StudentEnrollmentId = enrollment.StudentEnrollmentId,
                StudentId = enrollment.StudentId,
                RegisterNumber = enrollment.Student!.RegisterNumber,
                FirstName = enrollment.Student.FirstName,
                LastName = enrollment.Student.LastName
            }).ToListAsync();

        var enrollmentIds = students.Select(student => student.StudentEnrollmentId).ToList();
        var subjectIds = subjects.Select(subject => subject.SubjectId).ToList();
        var marks = await dbContext.StudentMarks.AsNoTracking()
            .Where(mark => !mark.IsDeleted && mark.ExamId == request.ExamId &&
                enrollmentIds.Contains(mark.StudentEnrollmentId) && subjectIds.Contains(mark.SubjectId))
            .Select(mark => new { mark.StudentEnrollmentId, mark.SubjectId, mark.MarksObtained, mark.Remarks })
            .ToListAsync();
        foreach (var student in students)
            student.Marks = subjects.Select(subject =>
            {
                var mark = marks.FirstOrDefault(value => value.StudentEnrollmentId == student.StudentEnrollmentId &&
                    value.SubjectId == subject.SubjectId);
                return new StudentMarksValueDto
                {
                    SubjectId = subject.SubjectId,
                    MarksObtained = mark?.MarksObtained,
                    Remarks = mark?.Remarks
                };
            }).ToList();

        return new StudentMarksFetchResponse
        {
            ExamId = exam.ExamId,
            ExamName = exam.ExamName,
            TotalCount = totalCount,
            Limit = request.Limit,
            Offset = request.Offset,
            Subjects = subjects,
            Students = students
        };
    }

    public async Task SaveMarksAsync(StudentMarksManageRequest request, int employeeId)
    {
        if (request.StudentEnrollmentId <= 0) throw new ArgumentException("StudentEnrollmentId is required.");
        if (request.ExamId <= 0) throw new ArgumentException("ExamId is required.");
        if (request.SubjectMarks.Count == 0) throw new ArgumentException("At least one subject mark is required.");
        if (request.SubjectMarks.Any(mark => mark.SubjectId <= 0)) throw new ArgumentException("Every SubjectId must be valid.");
        if (request.SubjectMarks.Any(mark => mark.MarksObtained < 0)) throw new ArgumentException("Marks cannot be negative.");
        if (request.SubjectMarks.Any(mark => mark.Remarks?.Length > 250)) throw new ArgumentException("Remarks cannot exceed 250 characters.");
        if (request.SubjectMarks.Select(mark => mark.SubjectId).Distinct().Count() != request.SubjectMarks.Count)
            throw new ArgumentException("A subject cannot appear more than once.");
        if (!await dbContext.Employees.AnyAsync(employee => employee.EmployeeId == employeeId && !employee.IsDeleted))
            throw new UnauthorizedAccessException("Staff employee was not found.");

        var enrollment = await dbContext.StudentEnrollments.AsNoTracking().FirstOrDefaultAsync(item =>
            item.StudentEnrollmentId == request.StudentEnrollmentId && !item.IsDeleted)
            ?? throw new KeyNotFoundException("Student enrollment not found.");
        var exam = await GetMatchingExamAsync(request.ExamId, enrollment.ClassName, enrollment.SectionName,
            enrollment.GroupName, enrollment.AcademicYear);
        if (exam.IsPublished) throw new InvalidOperationException("Published exam marks cannot be changed.");

        var requestedIds = request.SubjectMarks.Select(mark => mark.SubjectId).ToList();
        var allowedSubjects = await dbContext.ClassSubjectMappings.AsNoTracking()
            .Where(mapping => !mapping.IsDeleted && mapping.ClassName == enrollment.ClassName &&
                mapping.GroupName == enrollment.GroupName && mapping.AcademicYear == enrollment.AcademicYear &&
                (mapping.SectionName == null || mapping.SectionName == enrollment.SectionName) &&
                requestedIds.Contains(mapping.SubjectId) && mapping.Subject != null && !mapping.Subject.IsDeleted)
            .Select(mapping => new { mapping.SubjectId, mapping.Subject!.MaximumMarks }).Distinct().ToListAsync();
        if (allowedSubjects.Select(subject => subject.SubjectId).Distinct().Count() != requestedIds.Count)
            throw new InvalidOperationException("One or more subjects are not mapped to this student's class.");
        foreach (var value in request.SubjectMarks)
        {
            var subject = allowedSubjects.First(item => item.SubjectId == value.SubjectId);
            if (value.MarksObtained > subject.MaximumMarks)
                throw new ArgumentException($"Marks for SubjectId {value.SubjectId} cannot exceed {subject.MaximumMarks}.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var existingMarks = await dbContext.StudentMarks.Where(mark =>
            mark.StudentEnrollmentId == request.StudentEnrollmentId && mark.ExamId == request.ExamId &&
            requestedIds.Contains(mark.SubjectId)).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var value in request.SubjectMarks)
        {
            var existing = existingMarks.FirstOrDefault(mark => mark.SubjectId == value.SubjectId);
            if (existing is null)
            {
                dbContext.StudentMarks.Add(new StudentMark
                {
                    StudentEnrollmentId = request.StudentEnrollmentId,
                    ExamId = request.ExamId,
                    SubjectId = value.SubjectId,
                    MarksObtained = value.MarksObtained,
                    EnteredByEmployeeId = employeeId,
                    Remarks = Normalize(value.Remarks),
                    CreatedAt = now
                });
            }
            else
            {
                existing.MarksObtained = value.MarksObtained;
                existing.EnteredByEmployeeId = employeeId;
                existing.Remarks = Normalize(value.Remarks);
                existing.UpdatedAt = now;
                existing.IsDeleted = false;
            }
        }
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<int> GetStudentIdForUserAsync(int userId) =>
        await dbContext.Students.AsNoTracking()
            .Where(student => student.UserId == userId && !student.IsDeleted)
            .Select(student => (int?)student.StudentId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Student profile was not found for the logged-in user.");

    private async Task<Exam> GetMatchingExamAsync(int examId, string className, string sectionName,
        string groupName, string academicYear) =>
        await dbContext.Exams.AsNoTracking().FirstOrDefaultAsync(exam => !exam.IsDeleted &&
            exam.ExamId == examId && exam.ClassName == className && exam.GroupName == groupName &&
            exam.AcademicYear == academicYear && (exam.SectionName == null || exam.SectionName == sectionName))
        ?? throw new KeyNotFoundException("Exam was not found for the selected class.");

    private static void ValidateSelection(string? className, string? sectionName, string? groupName,
        string? academicYear, int examId, int limit, int offset)
    {
        if (string.IsNullOrWhiteSpace(className)) throw new ArgumentException("ClassName is required.");
        if (string.IsNullOrWhiteSpace(sectionName)) throw new ArgumentException("SectionName is required.");
        if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentException("GroupName is required.");
        if (string.IsNullOrWhiteSpace(academicYear)) throw new ArgumentException("AcademicYear is required.");
        if (examId <= 0) throw new ArgumentException("ExamId is required.");
        if (limit is < 1 or > 100) throw new ArgumentException("Limit must be between 1 and 100.");
        if (offset < 0) throw new ArgumentException("Offset cannot be negative.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
