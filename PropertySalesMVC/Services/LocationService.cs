using PropertySalesMVC.Models;
using PropertySalesMVC.Repositories;

namespace PropertySalesMVC.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepository;

        public LocationService(ILocationRepository locationRepository)
        {
            _locationRepository = locationRepository;
        }

        public Task<List<LocationOption>> GetAllForAdminAsync()
            => _locationRepository.GetAllLocationsForAdminAsync();

        public Task CreateAsync(string name)
            => _locationRepository.CreateLocationAsync(name);

        public Task UpdateAsync(int id, string name, bool isActive)
            => _locationRepository.UpdateLocationAsync(id, name, isActive);
    }
}
