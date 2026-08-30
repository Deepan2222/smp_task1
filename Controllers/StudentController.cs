using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smp_trask1.Handlers;
using smp_trask1.Models;
using Microsoft.AspNetCore.Authorization;

namespace smp_trask1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly StudentRegistrationService _registrationService;

        public StudentController(StudentRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        [HttpPost("manage")]
        public async Task<IActionResult> Manage([FromBody] StudentManageRequest request)
        {
            try
            {
                await _registrationService.ManageAsync(request);
                var response = OutputHandler.SuccessCode();
                response.responseMessage = request.Action switch
                {
                    0 => "Student registered successfully.",
                    1 => "Student updated successfully.",
                    2 => "Student deleted successfully.",
                    _ => "Student operation completed successfully."
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(200,
                    OutputHandler.Failure(ex.Message, "400"));
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] StudentResetPasswordRequest request)
        {
            try
            {
                await _registrationService.ResetPasswordAsync(request);
                var response = OutputHandler.SuccessCode();
                response.responseMessage = "Password updated successfully.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(200,
                    OutputHandler.Failure(ex.Message, "400"));
            }


        }

        
    }
}
