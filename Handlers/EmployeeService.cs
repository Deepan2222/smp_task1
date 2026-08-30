using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers
{
    public class EmployeeService
    {
        private readonly SmpDbContext _dbContext;
        private readonly UserRegister _userRegister;

        public EmployeeService(SmpDbContext dbContext, UserRegister userRegister)
        {
            _dbContext = dbContext;
            _userRegister = userRegister;
        }

        public async Task<List<Employee>> GetAllEmployees()
        {
            return await _dbContext.Employees
                .Where(employee => !employee.IsDeleted)
                .ToListAsync();
        }

        public async Task EmployeeManage(EmployeeManageDto request)
        {
            if (request.Action is null)
            {
                throw new ArgumentException("Action is required.");
            }

            switch (request.Action.Value)
            {
                case 0:
                    await AddEmployeeAsync(request);
                    break;
                case 1:
                    await UpdateEmployeeAsync(request);
                    break;
                case 2:
                    await SoftDeleteEmployeeAsync(request);
                    break;
                default:
                    throw new ArgumentException("Action must be 0 (add), 1 (update), or 2 (delete).");
            }
        }

        private async Task AddEmployeeAsync(EmployeeManageDto request)
        {
            ValidateEmployeeFields(request, requirePassword: true);
            await EnsureEmployeeCodeIsUniqueAsync(request.EmployeeCode!);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var user = await _userRegister.AddUserAsync(new UserRegisterDto
            {
                Email = request.Email!,
                Password = request.Password!,
                RoleId = request.RoleId,
                StatusId = request.StatusId
            });

            var employee = new Employee
            {
                UserId = user.UserId,
                EmployeeCode = request.EmployeeCode!.Trim(),
                FirstName = request.FirstName!.Trim(),
                LastName = NormalizeOptional(request.LastName),
                DateOfBirth = request.DateOfBirth!.Value,
                Gender = request.Gender!.Trim(),
                PhoneNumber = request.PhoneNumber!.Trim(),
                Address = request.Address!.Trim(),
                CityId = request.CityId,
                Designation = request.Designation!.Trim(),
                JoiningDate = request.JoiningDate!.Value,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task UpdateEmployeeAsync(EmployeeManageDto request)
        {
            if (request.EmployeeId is null or <= 0)
            {
                throw new ArgumentException("EmployeeId is required for update.");
            }

            ValidateEmployeeFields(request, requirePassword: false);
            var employee = await GetActiveEmployeeAsync(request.EmployeeId.Value);
            await EnsureEmployeeCodeIsUniqueAsync(request.EmployeeCode!, employee.EmployeeId);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            if (employee.UserId.HasValue)
            {
                await _userRegister.UpdateUserAsync(employee.UserId.Value, new UserUpdateDto
                {
                    Email = request.Email!,
                    RoleId = request.RoleId,
                    StatusId = request.StatusId
                });
            }

            employee.EmployeeCode = request.EmployeeCode!.Trim();
            employee.FirstName = request.FirstName!.Trim();
            employee.LastName = NormalizeOptional(request.LastName);
            employee.DateOfBirth = request.DateOfBirth!.Value;
            employee.Gender = request.Gender!.Trim();
            employee.PhoneNumber = request.PhoneNumber!.Trim();
            employee.Address = request.Address!.Trim();
            employee.CityId = request.CityId;
            employee.Designation = request.Designation!.Trim();
            employee.JoiningDate = request.JoiningDate!.Value;
            employee.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task SoftDeleteEmployeeAsync(EmployeeManageDto request)
        {
            if (request.EmployeeId is null or <= 0)
            {
                throw new ArgumentException("EmployeeId is required for delete.");
            }

            var employee = await GetActiveEmployeeAsync(request.EmployeeId.Value);
            var now = DateTime.UtcNow;
            employee.IsDeleted = true;
            employee.UpdatedAt = now;

            if (employee.UserId.HasValue)
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(item =>
                    item.UserId == employee.UserId.Value && !item.IsDeleted);
                if (user is not null)
                {
                    user.IsDeleted = true;
                    user.UpdatedAt = now;
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private static void ValidateEmployeeFields(EmployeeManageDto request, bool requirePassword)
        {
            Require(request.Email, "Email");
            if (requirePassword) Require(request.Password, "Password");
            Require(request.EmployeeCode, "EmployeeCode");
            Require(request.FirstName, "FirstName");
            if (request.DateOfBirth is null) throw new ArgumentException("DateOfBirth is required.");
            Require(request.Gender, "Gender");
            Require(request.PhoneNumber, "PhoneNumber");
            Require(request.Address, "Address");
            Require(request.Designation, "Designation");
            if (request.JoiningDate is null) throw new ArgumentException("JoiningDate is required.");

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
                throw new ArgumentException("Email is invalid.");
        }

        private static void Require(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} is required.");
        }

        private async Task<Employee> GetActiveEmployeeAsync(int employeeId)
        {
            return await _dbContext.Employees.FirstOrDefaultAsync(employee =>
                employee.EmployeeId == employeeId && !employee.IsDeleted)
                ?? throw new KeyNotFoundException("Employee not found.");
        }

        private async Task EnsureEmployeeCodeIsUniqueAsync(string employeeCode, int? excludedEmployeeId = null)
        {
            var normalizedCode = employeeCode.Trim();
            var exists = await _dbContext.Employees.AnyAsync(employee =>
                employee.EmployeeCode == normalizedCode &&
                employee.EmployeeId != excludedEmployeeId);

            if (exists) throw new InvalidOperationException("Employee code already exists.");
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public async Task<Object> GetEmployeeById(int id)
        {
            //var employee = await _dbContext.Employees.FirstOrDefaultAsync(emp => !emp.IsDeleted && emp.EmployeeId == id);
            //var empUser = await _dbContext.Users.FirstOrDefaultAsync(emp => !emp.IsDeleted && emp.UserId == employee.UserId);

            var employee = await _dbContext.Employees
                .Where(emp =>
                    !emp.IsDeleted &&
                    emp.EmployeeId == id &&
                    !emp.User.IsDeleted)
                .Select(emp => new
                {
                    emp.EmployeeId,
                    emp.EmployeeCode,
                    emp.FirstName,
                    emp.LastName,
                    emp.DateOfBirth,
                    emp.Gender,
                    emp.PhoneNumber,
                    emp.Address,
                    emp.CityId,
                    emp.Designation,
                    emp.JoiningDate,

                    User = new
                    {
                        emp.User.UserId,
                        emp.User.Email,
                        emp.User.RoleId,
                        emp.User.StatusId
                    }
                })
                .FirstOrDefaultAsync();

            if (employee==null)
            {
                throw new Exception("user not found");
            }

            return employee;
        }
    }
}
