using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smp_trask1.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentMarksStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetExamRanks
                    @ExamId INT,
                    @ClassName NVARCHAR(30),
                    @SectionName NVARCHAR(20),
                    @GroupName NVARCHAR(50),
                    @AcademicYear NVARCHAR(20),
                    @Offset INT,
                    @Limit INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH MappedSubjects AS
                    (
                        SELECT DISTINCT mapping.SubjectId
                        FROM ClassSubjectMappings mapping
                        INNER JOIN Subjects subject ON subject.SubjectId = mapping.SubjectId
                        WHERE mapping.IsDeleted = 0
                          AND subject.IsDeleted = 0
                          AND mapping.ClassName = @ClassName
                          AND mapping.GroupName = @GroupName
                          AND mapping.AcademicYear = @AcademicYear
                          AND (mapping.SectionName IS NULL OR mapping.SectionName = @SectionName)
                    ),
                    EligibleEnrollments AS
                    (
                        SELECT enrollment.StudentEnrollmentId, enrollment.StudentId
                        FROM StudentEnrollments enrollment
                        INNER JOIN Students student ON student.StudentId = enrollment.StudentId
                        WHERE enrollment.IsDeleted = 0
                          AND student.IsDeleted = 0
                          AND enrollment.ClassName = @ClassName
                          AND enrollment.SectionName = @SectionName
                          AND enrollment.GroupName = @GroupName
                          AND enrollment.AcademicYear = @AcademicYear
                    ),
                    StudentTotals AS
                    (
                        SELECT
                            enrollment.StudentEnrollmentId,
                            enrollment.StudentId,
                            CAST(COALESCE(SUM(mark.MarksObtained), 0) AS DECIMAL(18,2)) AS TotalObtained,
                            CAST(SUM(subject.MaximumMarks) AS DECIMAL(18,2)) AS TotalMaximum,
                            CAST(CASE WHEN COUNT(mark.StudentMarkId) = COUNT(*) THEN 1 ELSE 0 END AS BIT) AS IsComplete
                        FROM EligibleEnrollments enrollment
                        CROSS JOIN MappedSubjects mapped
                        INNER JOIN Subjects subject ON subject.SubjectId = mapped.SubjectId
                        LEFT JOIN StudentMarks mark
                            ON mark.StudentEnrollmentId = enrollment.StudentEnrollmentId
                           AND mark.ExamId = @ExamId
                           AND mark.SubjectId = mapped.SubjectId
                           AND mark.IsDeleted = 0
                        GROUP BY enrollment.StudentEnrollmentId, enrollment.StudentId
                    ),
                    RankedStudents AS
                    (
                        SELECT
                            totals.*,
                            CAST(totals.TotalObtained * 100.0 / NULLIF(totals.TotalMaximum, 0) AS DECIMAL(7,2)) AS Percentage,
                            DENSE_RANK() OVER (ORDER BY totals.TotalObtained DESC) AS StudentRank
                        FROM StudentTotals totals
                    )
                    SELECT
                        ranked.StudentId,
                        ranked.StudentEnrollmentId,
                        student.RegisterNumber,
                        LTRIM(RTRIM(CONCAT(student.FirstName, ' ', student.LastName))) AS StudentName,
                        ranked.TotalObtained,
                        ranked.TotalMaximum,
                        ranked.Percentage,
                        ranked.StudentRank,
                        ranked.IsComplete,
                        COUNT(*) OVER () AS TotalCount
                    FROM RankedStudents ranked
                    INNER JOIN Students student ON student.StudentId = ranked.StudentId
                    ORDER BY ranked.StudentRank, student.RegisterNumber
                    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
                END
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetStudentMarksheet
                    @ExamId INT,
                    @StudentId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    ;WITH ExamDetails AS
                    (
                        SELECT * FROM Exams
                        WHERE ExamId = @ExamId AND IsDeleted = 0 AND IsPublished = 1
                    ),
                    EligibleEnrollments AS
                    (
                        SELECT enrollment.*
                        FROM StudentEnrollments enrollment
                        CROSS JOIN ExamDetails exam
                        INNER JOIN Students student ON student.StudentId = enrollment.StudentId
                        WHERE enrollment.IsDeleted = 0
                          AND student.IsDeleted = 0
                          AND enrollment.ClassName = exam.ClassName
                          AND enrollment.GroupName = exam.GroupName
                          AND enrollment.AcademicYear = exam.AcademicYear
                          AND (exam.SectionName IS NULL OR enrollment.SectionName = exam.SectionName)
                    ),
                    EnrollmentSubjects AS
                    (
                        SELECT DISTINCT enrollment.StudentEnrollmentId, enrollment.StudentId, mapping.SubjectId
                        FROM EligibleEnrollments enrollment
                        INNER JOIN ClassSubjectMappings mapping
                            ON mapping.ClassName = enrollment.ClassName
                           AND mapping.GroupName = enrollment.GroupName
                           AND mapping.AcademicYear = enrollment.AcademicYear
                           AND (mapping.SectionName IS NULL OR mapping.SectionName = enrollment.SectionName)
                           AND mapping.IsDeleted = 0
                        INNER JOIN Subjects subject ON subject.SubjectId = mapping.SubjectId AND subject.IsDeleted = 0
                    ),
                    StudentTotals AS
                    (
                        SELECT
                            pair.StudentEnrollmentId,
                            pair.StudentId,
                            CAST(COALESCE(SUM(mark.MarksObtained), 0) AS DECIMAL(18,2)) AS TotalObtained,
                            CAST(SUM(subject.MaximumMarks) AS DECIMAL(18,2)) AS TotalMaximum,
                            SUM(CASE WHEN mark.StudentMarkId IS NULL THEN 1 ELSE 0 END) AS MissingCount,
                            SUM(CASE WHEN mark.StudentMarkId IS NOT NULL
                                      AND mark.MarksObtained < subject.MinimumPassMarks THEN 1 ELSE 0 END) AS FailedCount
                        FROM EnrollmentSubjects pair
                        INNER JOIN Subjects subject ON subject.SubjectId = pair.SubjectId
                        LEFT JOIN StudentMarks mark
                            ON mark.StudentEnrollmentId = pair.StudentEnrollmentId
                           AND mark.ExamId = @ExamId
                           AND mark.SubjectId = pair.SubjectId
                           AND mark.IsDeleted = 0
                        GROUP BY pair.StudentEnrollmentId, pair.StudentId
                    ),
                    RankedStudents AS
                    (
                        SELECT
                            totals.*,
                            CAST(totals.TotalObtained * 100.0 / NULLIF(totals.TotalMaximum, 0) AS DECIMAL(7,2)) AS Percentage,
                            DENSE_RANK() OVER (ORDER BY totals.TotalObtained DESC) AS StudentRank
                        FROM StudentTotals totals
                    )
                    SELECT
                        student.StudentId,
                        student.RegisterNumber,
                        LTRIM(RTRIM(CONCAT(student.FirstName, ' ', student.LastName))) AS StudentName,
                        enrollment.ClassName,
                        enrollment.SectionName,
                        enrollment.GroupName,
                        enrollment.AcademicYear,
                        exam.ExamId,
                        exam.ExamName,
                        subject.SubjectId,
                        subject.SubjectCode,
                        subject.SubjectName,
                        subject.MaximumMarks,
                        subject.MinimumPassMarks,
                        mark.MarksObtained,
                        mark.Remarks,
                        CASE
                            WHEN mark.StudentMarkId IS NULL THEN 'Pending'
                            WHEN mark.MarksObtained >= subject.MinimumPassMarks THEN 'Pass'
                            ELSE 'Fail'
                        END AS SubjectResult,
                        ranked.TotalObtained,
                        ranked.TotalMaximum,
                        ranked.Percentage,
                        CASE
                            WHEN ranked.MissingCount > 0 THEN 'Pending'
                            WHEN ranked.FailedCount > 0 THEN 'Fail'
                            ELSE 'Pass'
                        END AS OverallResult,
                        ranked.StudentRank
                    FROM RankedStudents ranked
                    INNER JOIN EligibleEnrollments enrollment
                        ON enrollment.StudentEnrollmentId = ranked.StudentEnrollmentId
                    INNER JOIN Students student ON student.StudentId = ranked.StudentId
                    INNER JOIN EnrollmentSubjects pair
                        ON pair.StudentEnrollmentId = ranked.StudentEnrollmentId
                    INNER JOIN Subjects subject ON subject.SubjectId = pair.SubjectId
                    CROSS JOIN ExamDetails exam
                    LEFT JOIN StudentMarks mark
                        ON mark.StudentEnrollmentId = ranked.StudentEnrollmentId
                       AND mark.ExamId = @ExamId
                       AND mark.SubjectId = pair.SubjectId
                       AND mark.IsDeleted = 0
                    WHERE ranked.StudentId = @StudentId
                    ORDER BY subject.SubjectName;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SP_GetStudentMarksheet;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SP_GetExamRanks;");
        }
    }
}
