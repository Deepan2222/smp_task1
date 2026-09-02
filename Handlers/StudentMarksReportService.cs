using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers;

public class StudentMarksReportService
{
    private readonly SmpDbContext _dbContext;

    public StudentMarksReportService(SmpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentRankResponse> GetRanksAsync(StudentRankRequest request)
    {
        ValidateRankRequest(request);
        var students = new List<StudentRankDto>();
        var totalCount = 0;

        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.SP_GetExamRanks");
        AddParameter(command, "@ExamId", request.ExamId);
        AddParameter(command, "@ClassName", request.ClassName!.Trim());
        AddParameter(command, "@SectionName", request.SectionName!.Trim());
        AddParameter(command, "@GroupName", request.GroupName!.Trim());
        AddParameter(command, "@AcademicYear", request.AcademicYear!.Trim());
        AddParameter(command, "@Offset", request.Offset);
        AddParameter(command, "@Limit", request.Limit);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
            students.Add(new StudentRankDto
            {
                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                StudentEnrollmentId = reader.GetInt32(reader.GetOrdinal("StudentEnrollmentId")),
                RegisterNumber = reader.GetString(reader.GetOrdinal("RegisterNumber")),
                StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                TotalObtained = reader.GetDecimal(reader.GetOrdinal("TotalObtained")),
                TotalMaximum = reader.GetDecimal(reader.GetOrdinal("TotalMaximum")),
                Percentage = reader.GetDecimal(reader.GetOrdinal("Percentage")),
                Rank = reader.GetInt64(reader.GetOrdinal("StudentRank")),
                IsComplete = reader.GetBoolean(reader.GetOrdinal("IsComplete"))
            });
        }

        return new StudentRankResponse
        {
            TotalCount = totalCount,
            Limit = request.Limit,
            Offset = request.Offset,
            Students = students
        };
    }

    public async Task<StudentMarksheetResponse> GetMarksheetAsync(int examId, int studentId)
    {
        if (examId <= 0) throw new ArgumentException("ExamId is required.");
        if (studentId <= 0) throw new ArgumentException("StudentId is required.");

        StudentMarksheetResponse? marksheet = null;
        var subjects = new List<StudentMarksheetSubjectDto>();
        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.SP_GetStudentMarksheet");
        AddParameter(command, "@ExamId", examId);
        AddParameter(command, "@StudentId", studentId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            marksheet ??= ReadMarksheet(reader);
            subjects.Add(ReadSubject(reader));
        }

        if (marksheet is null)
            throw new KeyNotFoundException("Published marksheet was not found.");

        marksheet.Subjects = subjects;
        return marksheet;
    }

    private static StudentMarksheetResponse ReadMarksheet(DbDataReader reader) => new()
    {
        StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
        RegisterNumber = reader.GetString(reader.GetOrdinal("RegisterNumber")),
        StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
        ClassName = reader.GetString(reader.GetOrdinal("ClassName")),
        SectionName = reader.GetString(reader.GetOrdinal("SectionName")),
        GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
        AcademicYear = reader.GetString(reader.GetOrdinal("AcademicYear")),
        ExamId = reader.GetInt32(reader.GetOrdinal("ExamId")),
        ExamName = reader.GetString(reader.GetOrdinal("ExamName")),
        TotalObtained = reader.GetDecimal(reader.GetOrdinal("TotalObtained")),
        TotalMaximum = reader.GetDecimal(reader.GetOrdinal("TotalMaximum")),
        Percentage = reader.GetDecimal(reader.GetOrdinal("Percentage")),
        OverallResult = reader.GetString(reader.GetOrdinal("OverallResult")),
        Rank = reader.GetInt64(reader.GetOrdinal("StudentRank"))
    };

    private static StudentMarksheetSubjectDto ReadSubject(DbDataReader reader)
    {
        var marksIndex = reader.GetOrdinal("MarksObtained");
        var remarksIndex = reader.GetOrdinal("Remarks");
        return new StudentMarksheetSubjectDto
        {
            SubjectId = reader.GetInt32(reader.GetOrdinal("SubjectId")),
            SubjectCode = reader.GetString(reader.GetOrdinal("SubjectCode")),
            SubjectName = reader.GetString(reader.GetOrdinal("SubjectName")),
            MaximumMarks = reader.GetDecimal(reader.GetOrdinal("MaximumMarks")),
            MinimumPassMarks = reader.GetDecimal(reader.GetOrdinal("MinimumPassMarks")),
            MarksObtained = reader.IsDBNull(marksIndex) ? null : reader.GetDecimal(marksIndex),
            Result = reader.GetString(reader.GetOrdinal("SubjectResult")),
            Remarks = reader.IsDBNull(remarksIndex) ? null : reader.GetString(remarksIndex)
        };
    }

    private static DbCommand CreateCommand(DbConnection connection, string procedureName)
    {
        var command = connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value) =>
        command.Parameters.Add(new SqlParameter(name, value));

    private static void ValidateRankRequest(StudentRankRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClassName)) throw new ArgumentException("ClassName is required.");
        if (string.IsNullOrWhiteSpace(request.SectionName)) throw new ArgumentException("SectionName is required.");
        if (string.IsNullOrWhiteSpace(request.GroupName)) throw new ArgumentException("GroupName is required.");
        if (string.IsNullOrWhiteSpace(request.AcademicYear)) throw new ArgumentException("AcademicYear is required.");
        if (request.ExamId <= 0) throw new ArgumentException("ExamId is required.");
        if (request.Limit is < 1 or > 100) throw new ArgumentException("Limit must be between 1 and 100.");
        if (request.Offset < 0) throw new ArgumentException("Offset cannot be negative.");
    }
}
