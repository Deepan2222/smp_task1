using Microsoft.EntityFrameworkCore;
using smp_trask1.Data;
using smp_trask1.Models;

namespace smp_trask1.Handlers
{
    public class LocationService 
    {
        private readonly SmpDbContext _dbContext;

        public LocationService(SmpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // No parameter: return countries.
        public async Task<List<LocationResponse>> GetLocationsAsync()
        {
            return await _dbContext.Countries
                .AsNoTracking()
                .Where(country => !country.IsDeleted)
                .OrderBy(country => country.CountryName)
                .Select(country => new LocationResponse
                {
                    Id = country.CountryId,
                    Name = country.CountryName
                })
                .ToListAsync();
        }

        // One parameter: return states for a country.
        public async Task<List<LocationResponse>> GetLocationsAsync(int countryId)
        {
            var countryExists = await _dbContext.Countries.AnyAsync(country =>
                country.CountryId == countryId && !country.IsDeleted);

            if (!countryExists)
                throw new KeyNotFoundException("Country not found.");

            return await _dbContext.States
                .AsNoTracking()
                .Where(state => state.CountryId == countryId && !state.IsDeleted)
                .OrderBy(state => state.StateName)
                .Select(state => new LocationResponse
                {
                    Id = state.StateId,
                    Name = state.StateName
                })
                .ToListAsync();
        }

        // Two parameters: return cities after confirming the state belongs to the country.
        public async Task<List<LocationResponse>> GetLocationsAsync(int countryId, int stateId)
        {
            var stateExists = await _dbContext.States.AnyAsync(state =>
                state.StateId == stateId &&
                state.CountryId == countryId &&
                !state.IsDeleted);

            if (!stateExists)
                throw new KeyNotFoundException("State not found for the selected country.");

            return await _dbContext.Cities
                .AsNoTracking()
                .Where(city => city.StateId == stateId && !city.IsDeleted)
                .OrderBy(city => city.CityName)
                .Select(city => new LocationResponse
                {
                    Id = city.CityId,
                    Name = city.CityName
                })
                .ToListAsync();
        }
    }
}
