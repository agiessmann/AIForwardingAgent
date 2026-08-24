using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public interface IFreightLocationService
{
    Task<FreightLocation> CreateFreightLocationAsync(FreightLocation freightLocation);
    Task<FreightLocation?> GetFreightLocationByIdAsync(int id);
    Task<IEnumerable<FreightLocation>> GetAllFreightLocationsAsync();
    Task<FreightLocation> UpdateFreightLocationAsync(FreightLocation freightLocation);
    Task<bool> DeleteFreightLocationAsync(FreightLocation freightLocation);
}

public class FreightLocationService(IRepositoryService<FreightLocation> repositoryService) : IFreightLocationService
{
    // CRUD operations for FreightLocation entity
    public async Task<FreightLocation> CreateFreightLocationAsync(FreightLocation freightLocation)
    {
        return await repositoryService.Create(freightLocation);
    }  
    
    public async Task<FreightLocation?> GetFreightLocationByIdAsync(int id)
    {
        return await repositoryService.GetById(id);
    }
    
    public async Task<IEnumerable<FreightLocation>> GetAllFreightLocationsAsync()
    {
        return await repositoryService.GetAll();
    }
    
    public async Task<FreightLocation> UpdateFreightLocationAsync(FreightLocation freightLocation)
    {
        return await repositoryService.Update(freightLocation);
    }
    
    public async Task<bool> DeleteFreightLocationAsync(FreightLocation freightLocation)
    {
        return  await repositoryService.Delete(freightLocation);
    }
    
}