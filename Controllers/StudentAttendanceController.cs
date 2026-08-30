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
    public class StudentAttendanceController : ControllerBase
    {
        private readonly StudentAttendanceService _attendanceService;

        public StudentAttendanceController(StudentAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpPost("students")]
        public async Task<IActionResult> GetStudents(
            [FromBody] StudentAttendanceStudentListRequest request)
        {
            try
            {
                var result = await _attendanceService.GetStudentsAsync(request);
                return Ok(OutputHandler.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(200,
                    OutputHandler.Failure(ex.Message, "400"));
            }
        }

        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance(
            [FromBody] StudentAttendanceMarkRequest request)
        {
            try
            {
                await _attendanceService.MarkAttendanceAsync(request);
                var response = OutputHandler.SuccessCode();
                response.responseMessage = "Student attendance marked successfully.";
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
