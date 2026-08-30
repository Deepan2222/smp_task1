namespace smp_trask1.Models;

public sealed class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public LoginUserDto User { get; set; } = null!;
}

public sealed class LoginUserDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? StatusId { get; set; }
    public int? EmployeeId { get; set; }
    public int? StudentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
