using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Handlers;
using smp_trask1.Models;
using smp_trask1.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthHandler _authHandler;

    public AuthController(AuthHandler authHandler)
    {
        _authHandler = authHandler;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authHandler.AuthenticateAsync(request.Email, request.Password);

            return Ok(OutputHandler.Success(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(200,
                OutputHandler.Failure(ex.Message, "401"));
        }
        catch (Exception ex)
        {
            return StatusCode(200, OutputHandler.Failure(ex.Message, "400"));
        }
    }
}
