using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using smp_trask1.Handlers;
using smp_trask1.Models;
using System.Security.Claims;

namespace smp_trask1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentMarksController : ControllerBase
{
    private readonly StudentMarksService _studentMarksService;
    private readonly StudentMarksReportService _reportService;

    public StudentMarksController(
        StudentMarksService studentMarksService,
        StudentMarksReportService reportService)
    {
        _studentMarksService = studentMarksService;
        _reportService = reportService;
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> Fetch([FromBody] StudentMarksFetchRequest request)
    {
        if (!TryEmployeeId(out _)) return StaffUnauthorized();
        return await ExecuteAsync(async () => OutputHandler.Success(await _studentMarksService.FetchAsync(request)));
    }

    [HttpPost("manage")]
    public async Task<IActionResult> Manage([FromBody] StudentMarksManageRequest request)
    {
        if (!TryEmployeeId(out var employeeId)) return StaffUnauthorized();
        return await ExecuteAsync(async () =>
        {
            await _studentMarksService.SaveMarksAsync(request, employeeId);
            var response = OutputHandler.SuccessCode();
            response.responseMessage = "Student marks saved successfully.";
            return response;
        });
    }

    [HttpPost("ranks")]
    public async Task<IActionResult> Ranks([FromBody] StudentRankRequest request)
    {
        if (!TryEmployeeId(out _)) return StaffUnauthorized();
        return await ExecuteAsync(async () => OutputHandler.Success(await _reportService.GetRanksAsync(request)));
    }

    [HttpGet("my-marksheet/{examId:int}")]
    public async Task<IActionResult> MyMarksheet(int examId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized(OutputHandler.Failure("Student authentication is required.", "401"));
        return await ExecuteAsync(async () =>
        {
            var studentId = await _studentMarksService.GetStudentIdForUserAsync(userId);
            return OutputHandler.Success(await _reportService.GetMarksheetAsync(examId, studentId));
        });
    }

    [HttpPost("marksheet")]
    public async Task<IActionResult> Marksheet([FromBody] StudentMarksheetRequest request)
    {
        if (request.ExamId <= 0 || request.StudentId <= 0)
            return Ok(OutputHandler.Failure("ExamId and StudentId are required."));

        var isStaff = TryEmployeeId(out _);
        if (!isStaff)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized(OutputHandler.Failure("Authentication is required.", "401"));

            try
            {
                var authenticatedStudentId = await _studentMarksService.GetStudentIdForUserAsync(userId);
                if (authenticatedStudentId != request.StudentId)
                    return StatusCode(403,
                        OutputHandler.Failure("You can only view your own marksheet.", "403"));
            }
            catch (Exception ex)
            {
                return Ok(OutputHandler.Failure(ex.Message));
            }
        }

        return await ExecuteAsync(async () =>
            OutputHandler.Success(await _reportService.GetMarksheetAsync(
                request.ExamId, request.StudentId)));
    }

    private bool TryEmployeeId(out int employeeId) =>
        int.TryParse(User.FindFirst("employeeId")?.Value, out employeeId);
    private IActionResult StaffUnauthorized() =>
        Unauthorized(OutputHandler.Failure("Staff authentication is required.", "401"));
    private async Task<IActionResult> ExecuteAsync(Func<Task<OutputHandler>> action)
    {
        try { return Ok(await action()); }
        catch (Exception ex) { return Ok(OutputHandler.Failure(ex.Message)); }
    }
}
