using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smp_trask1.Migrations
{
    /// <inheritdoc />
    public partial class addedTablesForTaskTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "StudentEnrollments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "General"),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExamDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.ExamId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaximumMarks = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MinimumPassMarks = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                    table.CheckConstraint("CK_Subject_MaximumMarks", "[MaximumMarks] > 0");
                    table.CheckConstraint("CK_Subject_MinimumPassMarks", "[MinimumPassMarks] >= 0 AND [MinimumPassMarks] <= [MaximumMarks]");
                });

            migrationBuilder.CreateTable(
                name: "ClassSubjectMappings",
                columns: table => new
                {
                    ClassSubjectMappingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "General"),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSubjectMappings", x => x.ClassSubjectMappingId);
                    table.ForeignKey(
                        name: "FK_ClassSubjectMappings_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentMarks",
                columns: table => new
                {
                    StudentMarkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    MarksObtained = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EnteredByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMarks", x => x.StudentMarkId);
                    table.CheckConstraint("CK_StudentMark_MarksObtained", "[MarksObtained] >= 0");
                    table.ForeignKey(
                        name: "FK_StudentMarks_Employees_EnteredByEmployeeId",
                        column: x => x.EnteredByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMarks_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjectMappings_ClassName_SectionName_GroupName_AcademicYear_SubjectId",
                table: "ClassSubjectMappings",
                columns: new[] { "ClassName", "SectionName", "GroupName", "AcademicYear", "SubjectId" },
                unique: true,
                filter: "[SectionName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjectMappings_SubjectId",
                table: "ClassSubjectMappings",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamName_ClassName_SectionName_GroupName_AcademicYear",
                table: "Exams",
                columns: new[] { "ExamName", "ClassName", "SectionName", "GroupName", "AcademicYear" },
                unique: true,
                filter: "[SectionName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_EnteredByEmployeeId",
                table: "StudentMarks",
                column: "EnteredByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_ExamId",
                table: "StudentMarks",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_StudentEnrollmentId_ExamId_SubjectId",
                table: "StudentMarks",
                columns: new[] { "StudentEnrollmentId", "ExamId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_SubjectId",
                table: "StudentMarks",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects",
                column: "SubjectCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassSubjectMappings");

            migrationBuilder.DropTable(
                name: "StudentMarks");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "StudentEnrollments");
        }
    }
}
