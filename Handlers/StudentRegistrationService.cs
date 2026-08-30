using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers
{
    public class StudentRegistrationService
    {
        private readonly SmpDbContext _dbContext;
        private readonly UserRegister _userRegister;
        private readonly string _allowedFileUrlPrefix;

        public StudentRegistrationService(
            SmpDbContext dbContext,
            UserRegister userRegister,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _userRegister = userRegister;
            _allowedFileUrlPrefix = configuration["SupabaseStorage:PublicObjectUrl"]
                ?? throw new InvalidOperationException("Storage public URL is missing.");
        }

        public async Task ManageAsync(StudentManageRequest request)
        {
            if (request.Action is null)
                throw new ArgumentException("Action is required.");

            switch (request.Action.Value)
            {
                case 0:
                    await RegisterAsync(request);
                    break;
                case 1:
                    await UpdateAsync(request);
                    break;
                case 2:
                    await SoftDeleteAsync(request);
                    break;
                default:
                    throw new ArgumentException("Action must be 0 (add), 1 (update), or 2 (delete).");
            }
        }

        public async Task ResetPasswordAsync(StudentResetPasswordRequest request)
        {
            var student = await GetActiveStudentAsync(request.StudentId);

            if (!student.UserId.HasValue)
                throw new KeyNotFoundException("Student user account not found.");

            await _userRegister.ResetPasswordAsync(student.UserId.Value, new ResetPasswordDto
            {
                OldPassword = request.OldPassword,
                NewPassword = request.NewPassword
            });
        }

        private async Task RegisterAsync(StudentManageRequest request)
        {
            ValidateStudentFields(request, requirePassword: true);
            ValidateDocuments(request.Documents);

            var registerNumber = request.RegisterNumber!.Trim();
            var registerNumberExists = await _dbContext.Students.AnyAsync(student =>
                student.RegisterNumber == registerNumber);

            if (registerNumberExists)
                throw new InvalidOperationException("Register number already exists.");

            await ValidateReferencesAsync(request);

            var documentTypeIds = request.Documents
                .Select(document => document.StudentDocumentTypeId)
                .ToList();

            var documentTypes = await _dbContext.StudentDocumentTypes
                .Where(type => documentTypeIds.Contains(type.StudentDocumentTypeId) && !type.IsDeleted)
                .ToDictionaryAsync(type => type.StudentDocumentTypeId);

            if (documentTypes.Count != documentTypeIds.Count)
                throw new KeyNotFoundException("One or more student document types were not found.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var user = await _userRegister.AddUserAsync(new UserRegisterDto
            {
                Email = request.Email!,
                Password = request.Password!,
                RoleId = request.RoleId,
                StatusId = request.StatusId
            });

            var student = new Student
            {
                UserId = user.UserId,
                RegisterNumber = registerNumber,
                FirstName = request.FirstName!.Trim(),
                LastName = NormalizeOptional(request.LastName),
                DateOfBirth = request.DateOfBirth!.Value,
                Gender = request.Gender!.Trim(),
                PhoneNumber = request.PhoneNumber!.Trim(),
                Address = request.Address!.Trim(),
                CityId = request.CityId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Students.Add(student);
            await _dbContext.SaveChangesAsync();

            _dbContext.StudentEnrollments.Add(new StudentEnrollment
            {
                StudentId = student.StudentId,
                ClassName = request.ClassName!.Trim(),
                SectionName = request.SectionName!.Trim(),
                AcademicYear = request.AcademicYear!.Trim(),
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            });

            foreach (var document in request.Documents)
            {
                var documentType = documentTypes[document.StudentDocumentTypeId];
                _dbContext.StudentDocuments.Add(new StudentDocument
                {
                    StudentId = student.StudentId,
                    StudentDocumentTypeId = document.StudentDocumentTypeId,
                    DocumentName = documentType.DocumentTypeName,
                    FileUrl = document.FileUrl!.Trim(),
                    UploadedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task UpdateAsync(StudentManageRequest request)
        {
            if (request.StudentId is null or <= 0)
                throw new ArgumentException("StudentId is required for update.");

            ValidateStudentFields(request, requirePassword: false);
            if (request.Documents.Count > 0) ValidateDocuments(request.Documents);
            await ValidateReferencesAsync(request);

            var student = await GetActiveStudentAsync(request.StudentId.Value);
            var registerNumber = request.RegisterNumber!.Trim();
            var registerNumberExists = await _dbContext.Students.AnyAsync(item =>
                item.RegisterNumber == registerNumber && item.StudentId != student.StudentId);

            if (registerNumberExists)
                throw new InvalidOperationException("Register number already exists.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            if (student.UserId.HasValue)
            {
                await _userRegister.UpdateUserAsync(student.UserId.Value, new UserUpdateDto
                {
                    Email = request.Email!,
                    RoleId = request.RoleId,
                    StatusId = request.StatusId
                });
            }

            student.RegisterNumber = registerNumber;
            student.FirstName = request.FirstName!.Trim();
            student.LastName = NormalizeOptional(request.LastName);
            student.DateOfBirth = request.DateOfBirth!.Value;
            student.Gender = request.Gender!.Trim();
            student.PhoneNumber = request.PhoneNumber!.Trim();
            student.Address = request.Address!.Trim();
            student.CityId = request.CityId;
            student.UpdatedAt = DateTime.UtcNow;

            var enrollment = await _dbContext.StudentEnrollments.FirstOrDefaultAsync(item =>
                item.StudentId == student.StudentId && item.IsCurrent && !item.IsDeleted)
                ?? throw new KeyNotFoundException("Current student enrollment not found.");

            enrollment.ClassName = request.ClassName!.Trim();
            enrollment.SectionName = request.SectionName!.Trim();
            enrollment.AcademicYear = request.AcademicYear!.Trim();
            enrollment.UpdatedAt = DateTime.UtcNow;

            await UpdateDocumentsAsync(student.StudentId, request.Documents);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task SoftDeleteAsync(StudentManageRequest request)
        {
            if (request.StudentId is null or <= 0)
                throw new ArgumentException("StudentId is required for delete.");

            var student = await GetActiveStudentAsync(request.StudentId.Value);
            var now = DateTime.UtcNow;
            student.IsDeleted = true;
            student.UpdatedAt = now;

            if (student.UserId.HasValue)
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(item =>
                    item.UserId == student.UserId && !item.IsDeleted);
                if (user is not null)
                {
                    user.IsDeleted = true;
                    user.UpdatedAt = now;
                }
            }

            var enrollments = await _dbContext.StudentEnrollments
                .Where(item => item.StudentId == student.StudentId && !item.IsDeleted)
                .ToListAsync();
            foreach (var enrollment in enrollments)
            {
                enrollment.IsDeleted = true;
                enrollment.IsCurrent = false;
                enrollment.UpdatedAt = now;
            }

            var documents = await _dbContext.StudentDocuments
                .Where(item => item.StudentId == student.StudentId && !item.IsDeleted)
                .ToListAsync();
            foreach (var document in documents)
                document.IsDeleted = true;

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateDocumentsAsync(
            int studentId, List<StudentRegisterDocumentRequest> documents)
        {
            if (documents.Count == 0) return;

            var typeIds = documents.Select(item => item.StudentDocumentTypeId).ToList();
            var documentTypes = await _dbContext.StudentDocumentTypes
                .Where(type => typeIds.Contains(type.StudentDocumentTypeId) && !type.IsDeleted)
                .ToDictionaryAsync(type => type.StudentDocumentTypeId);

            if (documentTypes.Count != typeIds.Count)
                throw new KeyNotFoundException("One or more student document types were not found.");

            foreach (var requestDocument in documents)
            {
                var document = await _dbContext.StudentDocuments.FirstOrDefaultAsync(item =>
                    item.StudentId == studentId &&
                    item.StudentDocumentTypeId == requestDocument.StudentDocumentTypeId);

                if (document is null)
                {
                    _dbContext.StudentDocuments.Add(new StudentDocument
                    {
                        StudentId = studentId,
                        StudentDocumentTypeId = requestDocument.StudentDocumentTypeId,
                        DocumentName = documentTypes[requestDocument.StudentDocumentTypeId].DocumentTypeName,
                        FileUrl = requestDocument.FileUrl!.Trim(),
                        UploadedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    document.FileUrl = requestDocument.FileUrl!.Trim();
                    document.DocumentName = documentTypes[requestDocument.StudentDocumentTypeId].DocumentTypeName;
                    document.UploadedAt = DateTime.UtcNow;
                    document.IsDeleted = false;
                }
            }
        }

        private async Task ValidateReferencesAsync(StudentManageRequest request)
        {
            if (request.CityId.HasValue && !await _dbContext.Cities.AnyAsync(city =>
                    city.CityId == request.CityId && !city.IsDeleted))
                throw new KeyNotFoundException("City not found.");

            if (request.RoleId.HasValue && !await _dbContext.Roles.AnyAsync(role =>
                    role.RoleId == request.RoleId && !role.IsDeleted))
                throw new KeyNotFoundException("Role not found.");

            if (request.StatusId.HasValue && !await _dbContext.Statuses.AnyAsync(status =>
                    status.StatusId == request.StatusId && !status.IsDeleted))
                throw new KeyNotFoundException("Status not found.");
        }

        private static void ValidateStudentFields(StudentManageRequest request, bool requirePassword)
        {
            Require(request.Email, "Email");
            if (requirePassword) Require(request.Password, "Password");
            Require(request.RegisterNumber, "RegisterNumber");
            Require(request.FirstName, "FirstName");
            if (!request.DateOfBirth.HasValue)
                throw new ArgumentException("DateOfBirth is required.");
            Require(request.Gender, "Gender");
            Require(request.PhoneNumber, "PhoneNumber");
            Require(request.Address, "Address");
            Require(request.ClassName, "ClassName");
            Require(request.SectionName, "SectionName");
            Require(request.AcademicYear, "AcademicYear");

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
                throw new ArgumentException("Email is invalid.");
        }

        private async Task<Student> GetActiveStudentAsync(int studentId)
        {
            return await _dbContext.Students.FirstOrDefaultAsync(student =>
                student.StudentId == studentId && !student.IsDeleted)
                ?? throw new KeyNotFoundException("Student not found.");
        }

        private void ValidateDocuments(List<StudentRegisterDocumentRequest> documents)
        {
            if (documents.Count == 0)
                throw new ArgumentException("At least one student photo or document is required.");

            var duplicateType = documents
                .GroupBy(document => document.StudentDocumentTypeId)
                .Any(group => group.Count() > 1);

            if (duplicateType)
                throw new ArgumentException("The same document type cannot be added more than once.");

            foreach (var document in documents)
            {
                if (string.IsNullOrWhiteSpace(document.FileUrl) ||
                    !document.FileUrl.StartsWith(_allowedFileUrlPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("One or more file URLs are invalid.");
            }
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void Require(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} is required.");
        }
    }
}
