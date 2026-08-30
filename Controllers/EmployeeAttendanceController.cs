using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smp_trask1.Handlers;
using smp_trask1.Models;

namespace smp_trask1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeAttendanceController : ControllerBase
    {
        private readonly EmployeeAttendanceService _attendanceService;

        public EmployeeAttendanceController(EmployeeAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpPost]
        public async Task<IActionResult> Manage([FromBody] EmployeeAttendanceRequest request)
        {
            try
            {
                await _attendanceService.ManageAsync(request);

                var response = OutputHandler.SuccessCode();
                response.responseMessage = request.Action == 1
                    ? "Employee checked in successfully."
                    : "Employee checked out successfully.";

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500,OutputHandler.Failure(ex.Message, "500"));
            }
        }

        [HttpPost("edit")]
        public async Task<IActionResult> Edit([FromBody] EmployeeAttendanceEditRequest request)
        {
            try
            {
                await _attendanceService.EditAsync(request);

                var response = OutputHandler.SuccessCode();
                response.responseMessage = "Employee attendance updated successfully.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, OutputHandler.Failure(ex.Message, "500"));
            }
        }
    }
}
