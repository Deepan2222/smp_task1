using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using smp_trask1.Handlers;
using smp_trask1.Models;

namespace smp_trask1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeService _employeeService;

        public EmployeeController(EmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpPost("manage")]
        public async Task<IActionResult> Manage([FromBody] EmployeeManageDto request)
        {
            try
            {
                await _employeeService.EmployeeManage(request);
                var response = OutputHandler.SuccessCode();
                response.responseMessage = request.Action switch
                {
                    0 => "Employee added successfully.",
                    1 => "Employee updated successfully.",
                    2 => "Employee deleted successfully.",
                    _ => "Employee operation completed successfully."
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(200,
                    OutputHandler.Failure(ex.Message, "400"));
            }
        }


        [HttpPost("getById")]
        public async Task<IActionResult> GetEmpById([FromBody] JObject request)
        {
            int id = request.Value<int>("id");

            if (id == 0) {
                return StatusCode(200,OutputHandler.Failure("id requried", "400"));
            }

            try
            {

                var emp = await _employeeService.GetEmployeeById(id);
                var response = OutputHandler.Success();
                response.responseResult = emp;
                return Ok(response);

            }
            catch (Exception ex)
            {
                return StatusCode(200, OutputHandler.Failure(ex.Message, "400"));
            }
        }

        [HttpGet("allEmployee")]
        public async Task<IActionResult> GetAllEmp() {
            try
            {

                var emp = await _employeeService.GetAllEmployees();
                var response = OutputHandler.Success();
                response.responseResult = emp;
                return Ok(response);

            }
            catch (Exception ex)
            {
                return StatusCode(200, OutputHandler.Failure(ex.Message, "400"));
            }
        }

    }
}
