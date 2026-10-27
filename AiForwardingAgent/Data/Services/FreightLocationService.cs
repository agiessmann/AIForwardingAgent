using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public interface IFreightLocationService
{
    Task<Location> CreateFreightLocationAsync(Location freightLocation);
    Task<Location?> GetFreightLocationByIdAsync(int id);
    Task<IEnumerable<Location>> GetAllFreightLocationsAsync();
    Task<Location> UpdateFreightLocationAsync(Location freightLocation);
    Task<bool> DeleteFreightLocationAsync(Location freightLocation);
}

public class FreightLocationService(IRepositoryService<Location> repositoryService) : IFreightLocationService
{
    // CRUD operations for FreightLocation entity
    public async Task<Location> CreateFreightLocationAsync(Location freightLocation)
    {
        return await repositoryService.Create(freightLocation);
    }  
    
    public async Task<Location?> GetFreightLocationByIdAsync(int id)
    {
        return await repositoryService.GetById(id);
    }
    
    public async Task<IEnumerable<Location>> GetAllFreightLocationsAsync()
    {
        return await repositoryService.GetAll();
    }
    
    public async Task<Location> UpdateFreightLocationAsync(Location freightLocation)
    {
        return await repositoryService.Update(freightLocation);
    }
    
    public async Task<bool> DeleteFreightLocationAsync(Location freightLocation)
    {
        return  await repositoryService.Delete(freightLocation);
    }
    
}