using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public interface IFreightPackageService
{
    Task<FreightPackage> CreateFreightPackageAsync(FreightPackage freightPackage);
    Task<FreightPackage?> GetFreightPackageByIdAsync(int id);
    Task<IEnumerable<FreightPackage>> GetAllFreightPackagesAsync();
    Task<FreightPackage> UpdateFreightPackageAsync(FreightPackage freightPackage);
    Task<bool> DeleteFreightPackageAsync(FreightPackage freightPackage);
}

public class FreightPackageService(IRepositoryService<FreightPackage> repositoryService) : IFreightPackageService
{
    // CRUD operations for FreightPackage entity
    public async Task<FreightPackage> CreateFreightPackageAsync(FreightPackage freightPackage)
    {
        return await repositoryService.Create(freightPackage);
    }
    
    public async Task<FreightPackage?> GetFreightPackageByIdAsync(int id)
    {
        return await repositoryService.GetById(id);
    }
    
    public async Task<IEnumerable<FreightPackage>> GetAllFreightPackagesAsync()
    {
        return await repositoryService.GetAll();
    }
    
    public async Task<FreightPackage> UpdateFreightPackageAsync(FreightPackage freightPackage)
    {
        return await repositoryService.Update(freightPackage);
    }
    
    public async Task<bool> DeleteFreightPackageAsync(FreightPackage freightPackage)
    {
        return  await repositoryService.Delete(freightPackage);
    }
    
}