using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers;

public class SubjectService
{
    private readonly SmpDbContext dbContext;

    public SubjectService(SmpDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<int> ManageAsync(SubjectManageRequest request)
    {
        if (request.Action is null)
            throw new ArgumentException("Action is required.");

        return request.Action.Value switch
        {
            0 => await AddAsync(request),
            1 => await EditAsync(request),
            2 => await DeleteAsync(request),
            _ => throw new ArgumentException("Action must be 0 (add), 1 (edit), or 2 (delete).")
        };
    }

    private async Task<int> AddAsync(SubjectManageRequest request)
    {
        ValidateSubject(request);
        var mappings = NormalizeMappings(request.Mappings);
        await EnsureCodeIsUniqueAsync(request.SubjectCode!);
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var subject = new Subject
        {
            SubjectCode = request.SubjectCode!.Trim(),
            SubjectName = request.SubjectName!.Trim(),
            MaximumMarks = request.MaximumMarks!.Value,
            MinimumPassMarks = request.MinimumPassMarks!.Value
        };
        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync();
        var now = DateTime.UtcNow;
        dbContext.ClassSubjectMappings.AddRange(mappings.Select(mapping => new ClassSubjectMapping
        {
            SubjectId = subject.SubjectId,
            ClassName = mapping.ClassName,
            SectionName = mapping.SectionName,
            GroupName = mapping.GroupName,
            AcademicYear = mapping.AcademicYear,
            CreatedAt = now
        }));
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return subject.SubjectId;
    }

    private async Task<int> EditAsync(SubjectManageRequest request)
    {
        var subjectId = RequireSubjectId(request);
        ValidateSubject(request);
        var requestedMappings = NormalizeMappings(request.Mappings);
        await EnsureCodeIsUniqueAsync(request.SubjectCode!, subjectId);
        var subject = await dbContext.Subjects.FirstOrDefaultAsync(subject =>
            subject.SubjectId == subjectId && !subject.IsDeleted)
            ?? throw new KeyNotFoundException("Subject not found.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        subject.SubjectCode = request.SubjectCode!.Trim();
        subject.SubjectName = request.SubjectName!.Trim();
        subject.MaximumMarks = request.MaximumMarks!.Value;
        subject.MinimumPassMarks = request.MinimumPassMarks!.Value;
        var existingMappings = await dbContext.ClassSubjectMappings
            .Where(mapping => mapping.SubjectId == subjectId).ToListAsync();
        var now = DateTime.UtcNow;

        foreach (var existing in existingMappings)
        {
            existing.IsDeleted = !requestedMappings.Any(requested => SameMapping(existing, requested));
            existing.UpdatedAt = now;
        }
        foreach (var requested in requestedMappings)
        {
            var existing = existingMappings.FirstOrDefault(item => SameMapping(item, requested));
            if (existing is not null)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
            }
            else
            {
                dbContext.ClassSubjectMappings.Add(new ClassSubjectMapping
                {
                    SubjectId = subjectId,
                    ClassName = requested.ClassName,
                    SectionName = requested.SectionName,
                    GroupName = requested.GroupName,
                    AcademicYear = requested.AcademicYear,
                    CreatedAt = now
                });
            }
        }
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return subjectId;
    }

    private async Task<int> DeleteAsync(SubjectManageRequest request)
    {
        var subjectId = RequireSubjectId(request);
        var subject = await dbContext.Subjects.FirstOrDefaultAsync(subject =>
            subject.SubjectId == subjectId && !subject.IsDeleted)
            ?? throw new KeyNotFoundException("Subject not found.");
        if (await dbContext.StudentMarks.AnyAsync(mark => mark.SubjectId == subjectId && !mark.IsDeleted))
            throw new InvalidOperationException("Subject cannot be deleted because student marks exist.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        subject.IsDeleted = true;
        var now = DateTime.UtcNow;
        var mappings = await dbContext.ClassSubjectMappings
            .Where(mapping => mapping.SubjectId == subjectId && !mapping.IsDeleted).ToListAsync();
        foreach (var mapping in mappings)
        {
            mapping.IsDeleted = true;
            mapping.UpdatedAt = now;
        }
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return subjectId;
    }

    private async Task EnsureCodeIsUniqueAsync(string code, int? excludedId = null)
    {
        var normalized = code.Trim();
        if (await dbContext.Subjects.AnyAsync(subject =>
                subject.SubjectCode == normalized && subject.SubjectId != excludedId))
            throw new InvalidOperationException("Subject code already exists.");
    }

    private static void ValidateSubject(SubjectManageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectCode)) throw new ArgumentException("SubjectCode is required.");
        if (string.IsNullOrWhiteSpace(request.SubjectName)) throw new ArgumentException("SubjectName is required.");
        if (request.MaximumMarks is null or <= 0) throw new ArgumentException("MaximumMarks must be greater than zero.");
        if (request.MinimumPassMarks is null or < 0) throw new ArgumentException("MinimumPassMarks cannot be negative.");
        if (request.MinimumPassMarks > request.MaximumMarks)
            throw new ArgumentException("MinimumPassMarks cannot exceed MaximumMarks.");
    }

    private static List<MappingValue> NormalizeMappings(List<SubjectMappingRequest>? mappings)
    {
        if (mappings is null || mappings.Count == 0)
            throw new ArgumentException("At least one mapping is required.");
        var result = mappings.Select(mapping =>
        {
            if (string.IsNullOrWhiteSpace(mapping.ClassName)) throw new ArgumentException("ClassName is required.");
            if (string.IsNullOrWhiteSpace(mapping.GroupName)) throw new ArgumentException("GroupName is required.");
            if (string.IsNullOrWhiteSpace(mapping.AcademicYear)) throw new ArgumentException("AcademicYear is required.");
            return new MappingValue(mapping.ClassName.Trim(),
                string.IsNullOrWhiteSpace(mapping.SectionName) ? null : mapping.SectionName.Trim(),
                mapping.GroupName.Trim(), mapping.AcademicYear.Trim());
        }).ToList();
        if (result.DistinctBy(item => $"{item.ClassName}|{item.SectionName}|{item.GroupName}|{item.AcademicYear}",
                StringComparer.OrdinalIgnoreCase).Count() != result.Count)
            throw new ArgumentException("Duplicate mappings are not allowed.");
        return result;
    }

    private static bool SameMapping(ClassSubjectMapping left, MappingValue right) =>
        string.Equals(left.ClassName, right.ClassName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.SectionName, right.SectionName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.GroupName, right.GroupName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.AcademicYear, right.AcademicYear, StringComparison.OrdinalIgnoreCase);

    private static int RequireSubjectId(SubjectManageRequest request) =>
        request.SubjectId is > 0 ? request.SubjectId.Value :
            throw new ArgumentException("SubjectId is required.");

    private sealed record MappingValue(string ClassName, string? SectionName, string GroupName, string AcademicYear);
}
