using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers
{
    public class EmployeeAttendanceService
    {
        private readonly SmpDbContext _dbContext;

        public EmployeeAttendanceService(SmpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ManageAsync(EmployeeAttendanceRequest request)
        {
            var employeeExists = await _dbContext.Employees.AnyAsync(employee =>
                employee.EmployeeId == request.EmployeeId && !employee.IsDeleted);

            if (!employeeExists)
                throw new KeyNotFoundException("Employee not found.");

            var statusExists = await _dbContext.AttendanceStatuses.AnyAsync(status =>
                status.AttendanceStatusId == request.AttendanceStatusId && !status.IsDeleted);

            if (!statusExists)
                throw new KeyNotFoundException("Attendance status not found.");

            if (request.Action == 1)
                await CheckInAsync(request);
            else if (request.Action == 2)
                await CheckOutAsync(request);
            else
                throw new ArgumentException("Action must be 1 (check-in) or 2 (check-out).");
        }

        private async Task CheckInAsync(EmployeeAttendanceRequest request)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            var attendanceExists = await _dbContext.EmployeeAttendances.AnyAsync(attendance =>
                attendance.EmployeeId == request.EmployeeId &&
                attendance.AttendanceDate == today);

            if (attendanceExists)
                throw new InvalidOperationException("Employee has already checked in today.");

            var attendance = new EmployeeAttendance
            {
                EmployeeId = request.EmployeeId,
                AttendanceDate = today,
                AttendanceStatusId = request.AttendanceStatusId,
                CheckInTime = request.AttendanceStatusId == 1 ? TimeOnly.FromDateTime(now) : null,
                Remarks = NormalizeRemarks(request.Remarks),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.EmployeeAttendances.Add(attendance);
            await _dbContext.SaveChangesAsync();
        }

        private async Task CheckOutAsync(EmployeeAttendanceRequest request)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            var attendance = await _dbContext.EmployeeAttendances.FirstOrDefaultAsync(item =>
                item.EmployeeId == request.EmployeeId &&
                item.AttendanceDate == today &&
                !item.IsDeleted);

            if (attendance is null || attendance.CheckInTime is null)
                throw new InvalidOperationException("Employee has not checked in today.");

            if (attendance.CheckOutTime.HasValue)
                throw new InvalidOperationException("Employee has already checked out today.");

            attendance.CheckOutTime = request.AttendanceStatusId == 1 ? TimeOnly.FromDateTime(now) : null;
            attendance.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Remarks))
                attendance.Remarks = NormalizeRemarks(request.Remarks);

            await _dbContext.SaveChangesAsync();
        }

        public async Task EditAsync(EmployeeAttendanceEditRequest request)
        {
            if (!request.AttendanceDate.HasValue)
                throw new ArgumentException("AttendanceDate is required.");

            var employeeExists = await _dbContext.Employees.AnyAsync(employee =>
                employee.EmployeeId == request.EmployeeId && !employee.IsDeleted);

            if (!employeeExists)
                throw new KeyNotFoundException("Employee not found.");

            var statusExists = await _dbContext.AttendanceStatuses.AnyAsync(status =>
                status.AttendanceStatusId == request.AttendanceStatusId && !status.IsDeleted);

            if (!statusExists)
                throw new KeyNotFoundException("Attendance status not found.");

            if (request.CheckOutTime.HasValue && !request.CheckInTime.HasValue)
                throw new ArgumentException("CheckInTime is required when CheckOutTime is provided.");

            if (request.CheckInTime.HasValue && request.CheckOutTime.HasValue &&
                request.CheckOutTime.Value < request.CheckInTime.Value)
                throw new ArgumentException("CheckOutTime cannot be earlier than CheckInTime.");

            var attendance = await _dbContext.EmployeeAttendances.FirstOrDefaultAsync(item =>
                item.EmployeeId == request.EmployeeId &&
                item.AttendanceDate == request.AttendanceDate.Value &&
                !item.IsDeleted);

            if (attendance is null)
                throw new KeyNotFoundException("Attendance was not found for the selected date.");

            attendance.AttendanceStatusId = request.AttendanceStatusId;
            attendance.CheckInTime = request.CheckInTime;
            attendance.CheckOutTime = request.CheckOutTime;
            attendance.Remarks = NormalizeRemarks(request.Remarks);
            attendance.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        private static string? NormalizeRemarks(string? remarks) =>
            string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }
}
