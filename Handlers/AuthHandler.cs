using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;
using smp_trask1.Services;

namespace smp_trask1.Handlers
{
    public class AuthHandler
    {
        private readonly SmpDbContext _context;
        private readonly PasswordHashService _passwordHasher;
        private readonly JwtTokenService _jwtTokenService;

        public AuthHandler(
            SmpDbContext context,
            PasswordHashService passwordHasher,
            JwtTokenService jwtTokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }


        public async Task<LoginResponseDto> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users
                                        .AsNoTracking()
                                        .Include(u => u.Role)
                                        .Include(u => u.Employee)
                                        .Include(u => u.Student)
                                        .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }
            var verificationResult = _passwordHasher.VerifyPassword(user.PasswordHash, password);

            if (!verificationResult)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var token = _jwtTokenService.GenerateToken(user);
            var profileFirstName = user.Employee?.FirstName ?? user.Student?.FirstName;
            var profileLastName = user.Employee?.LastName ?? user.Student?.LastName;

            return new LoginResponseDto
            {
                Token = token,
                User = new LoginUserDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    RoleId = user.RoleId,
                    RoleName = user.Role?.RoleName,
                    StatusId = user.StatusId,
                    EmployeeId = user.Employee?.EmployeeId,
                    StudentId = user.Student?.StudentId,
                    FirstName = profileFirstName,
                    LastName = profileLastName
                }
            };
        }
    }
}
