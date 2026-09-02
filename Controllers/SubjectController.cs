using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using smp_trask1.Handlers;
using smp_trask1.Models;

namespace smp_trask1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectController(SubjectService subjectService) : ControllerBase
{
    [HttpPost("manage")]
    public async Task<IActionResult> Manage([FromBody] SubjectManageRequest request)
    {
        if (!int.TryParse(User.FindFirst("employeeId")?.Value, out _))
            return Unauthorized(OutputHandler.Failure("Staff authentication is required.", "401"));
        try
        {
            var subjectId = await subjectService.ManageAsync(request);
            var response = OutputHandler.Success(new { subjectId });
            response.responseMessage = request.Action switch
            {
                0 => "Subject added successfully.",
                1 => "Subject updated successfully.",
                2 => "Subject deleted successfully.",
                _ => "Subject operation completed successfully."
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Ok(OutputHandler.Failure(ex.Message));
        }
    }
}
