namespace smp_trask1.Models;

public class SubjectManageRequest
{
    public int? Action { get; set; }
    public int? SubjectId { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public decimal? MaximumMarks { get; set; }
    public decimal? MinimumPassMarks { get; set; }
    public List<SubjectMappingRequest> Mappings { get; set; } = [];
}

public class SubjectMappingRequest
{
    public string? ClassName { get; set; }
    public string? SectionName { get; set; }
    public string? GroupName { get; set; } = "General";
    public string? AcademicYear { get; set; }
}

public class StudentMarksFetchRequest
{
    public string? ClassName { get; set; }
    public string? SectionName { get; set; }
    public string? GroupName { get; set; } = "General";
    public string? AcademicYear { get; set; }
    public int ExamId { get; set; }
    public int Limit { get; set; } = 20;
    public int Offset { get; set; }
}

public class StudentMarksSubjectDto
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public decimal MaximumMarks { get; set; }
    public decimal MinimumPassMarks { get; set; }
}

public class StudentMarksValueDto
{
    public int SubjectId { get; set; }
    public decimal? MarksObtained { get; set; }
    public string? Remarks { get; set; }
}

public class StudentMarksStudentDto
{
    public int StudentEnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string RegisterNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public List<StudentMarksValueDto> Marks { get; set; } = [];
}

public class StudentMarksFetchResponse
{
    public int ExamId { get; set; }
    public string ExamName { get; set; } = null!;
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public List<StudentMarksSubjectDto> Subjects { get; set; } = [];
    public List<StudentMarksStudentDto> Students { get; set; } = [];
}

public class StudentMarksManageRequest
{
    public int StudentEnrollmentId { get; set; }
    public int ExamId { get; set; }
    public List<StudentSubjectMarkRequest> SubjectMarks { get; set; } = [];
}

public class StudentSubjectMarkRequest
{
    public int SubjectId { get; set; }
    public decimal MarksObtained { get; set; }
    public string? Remarks { get; set; }
}

public class StudentRankRequest
{
    public string? ClassName { get; set; }
    public string? SectionName { get; set; }
    public string? GroupName { get; set; } = "General";
    public string? AcademicYear { get; set; }
    public int ExamId { get; set; }
    public int Limit { get; set; } = 20;
    public int Offset { get; set; }
}

public class StudentRankDto
{
    public int StudentId { get; set; }
    public int StudentEnrollmentId { get; set; }
    public string RegisterNumber { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public decimal TotalObtained { get; set; }
    public decimal TotalMaximum { get; set; }
    public decimal Percentage { get; set; }
    public long Rank { get; set; }
    public bool IsComplete { get; set; }
}

public class StudentRankResponse
{
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public List<StudentRankDto> Students { get; set; } = [];
}

public class StudentMarksheetSubjectDto
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public decimal MaximumMarks { get; set; }
    public decimal MinimumPassMarks { get; set; }
    public decimal? MarksObtained { get; set; }
    public string Result { get; set; } = null!;
    public string? Remarks { get; set; }
}

public class StudentMarksheetRequest
{
    public int ExamId { get; set; }
    public int StudentId { get; set; }
}

public class StudentMarksheetResponse
{
    public int StudentId { get; set; }
    public string RegisterNumber { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public string SectionName { get; set; } = null!;
    public string GroupName { get; set; } = null!;
    public string AcademicYear { get; set; } = null!;
    public int ExamId { get; set; }
    public string ExamName { get; set; } = null!;
    public decimal TotalObtained { get; set; }
    public decimal TotalMaximum { get; set; }
    public decimal Percentage { get; set; }
    public string OverallResult { get; set; } = null!;
    public long Rank { get; set; }
    public List<StudentMarksheetSubjectDto> Subjects { get; set; } = [];
}
