using Microsoft.AspNetCore.Mvc;
using smp_trask1.Models;
using smp_trask1.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace smp_trask1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly  LocationService _locationService;

        public LocationController( LocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpPost]
        public async Task<IActionResult> GetLocations([FromBody] LocationRequest request)
        {
            try
            {
                List<LocationResponse> locations;

                if (!request.CountryId.HasValue && !request.StateId.HasValue)
                {
                    locations = await _locationService.GetLocationsAsync();
                }
                else if (request.CountryId.HasValue && !request.StateId.HasValue)
                {
                    if (request.CountryId.Value <= 0)
                        throw new ArgumentException("CountryId must be valid.");

                    locations = await _locationService.GetLocationsAsync(request.CountryId.Value);
                }
                else if (request.CountryId.HasValue && request.StateId.HasValue)
                {
                    if (request.CountryId.Value <= 0 || request.StateId.Value <= 0)
                        throw new ArgumentException("CountryId and StateId must be valid.");

                    locations = await _locationService.GetLocationsAsync(
                        request.CountryId.Value, request.StateId.Value);
                }
                else
                {
                    throw new ArgumentException("CountryId is required when StateId is provided.");
                }

                return Ok(OutputHandler.Success(locations));
            }
            catch (Exception ex)
            {
                return StatusCode(200,
                    OutputHandler.Failure(ex.Message, "400"));
            }
        }
    }
}
