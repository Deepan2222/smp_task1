using Microsoft.AspNetCore.Mvc;
using smp_trask1.Handlers;
using smp_trask1.Models;
using Microsoft.AspNetCore.Authorization;


namespace smp_trask1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentDocumentController : ControllerBase
    {
        private readonly FileStorageService _fileStorageService;
        private readonly string _bucketName;

        public StudentDocumentController(
            FileStorageService fileStorageService,
            IConfiguration configuration)
        {
            _fileStorageService = fileStorageService;
            _bucketName = configuration["SupabaseStorage:BucketName"]
                ?? throw new InvalidOperationException("Storage bucket name is missing.");
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            try
            {
                var fileUrl = await _fileStorageService.UploadFileAsync(
                    file, _bucketName, "student-documents/temp");

                var response = OutputHandler.Success(new { fileUrl });
                response.responseMessage = "File uploaded successfully.";
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
