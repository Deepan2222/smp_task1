using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers
{
    public class UserRegister
    {
        private readonly SmpDbContext _dbContext;
        private readonly PasswordHashService _passwordHashService;

        public UserRegister(
            SmpDbContext dbContext,
            PasswordHashService  passwordHashService)
        {
            _dbContext = dbContext;
            _passwordHashService = passwordHashService;
        }

        public async Task<User> AddUserAsync(UserRegisterDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var emailExists = await _dbContext.Users
                .AnyAsync(user => user.Email == email);

            if (emailExists)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = _passwordHashService.HashPassword(request.Password),
                RoleId = request.RoleId,
                StatusId = request.StatusId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        public async Task<User> UpdateUserAsync(int userId, UserUpdateDto request)
        {
            var user = await GetActiveUserAsync(userId);
            var email = request.Email.Trim().ToLowerInvariant();

            var emailExists = await _dbContext.Users.AnyAsync(existingUser =>
                existingUser.UserId != userId &&
                existingUser.Email == email);

            if (emailExists)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            user.Email = email;
            user.RoleId = request.RoleId;
            user.StatusId = request.StatusId;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task ResetPasswordAsync(int userId, ResetPasswordDto request)
        {
            var user = await GetActiveUserAsync(userId);

            if (!_passwordHashService.VerifyPassword(request.OldPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("The old password is incorrect.");
            }

            user.PasswordHash = _passwordHashService.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        private async Task<User> GetActiveUserAsync(int userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser =>
                existingUser.UserId == userId && !existingUser.IsDeleted);

            return user ?? throw new KeyNotFoundException("User not found.");
        }
    }
}
