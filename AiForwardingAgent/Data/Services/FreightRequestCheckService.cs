using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public interface IFreightRequestCheckService
{
    Task<FreightRequestCheck> CreateFreightRequestCheckAsync(FreightRequestCheck freightRequestCheck);
    Task<FreightRequestCheck?> GetFreightRequestCheckByIdAsync(int id);
    Task<IEnumerable<FreightRequestCheck>> GetAllFreightRequestChecksAsync();
    Task<FreightRequestCheck> UpdateFreightRequestCheckAsync(FreightRequestCheck freightRequestCheck);
    Task DeleteFreightRequestCheckAsync(FreightRequestCheck freightRequestCheck);
}

public class FreightRequestCheckService(IRepositoryService<FreightRequestCheck> freightRequestCheckRepository) : IFreightRequestCheckService
{
    // Crud operations for FreightRequestCheck can be implemented here using the freightRequestCheckRepository
    
    public async Task<FreightRequestCheck> CreateFreightRequestCheckAsync(FreightRequestCheck freightRequestCheck)
    {
        return await freightRequestCheckRepository.Create(freightRequestCheck);
    }
    
    public async Task<FreightRequestCheck?> GetFreightRequestCheckByIdAsync(int id)
    {
        return await freightRequestCheckRepository.GetById(id);
    }
    
    public async Task<IEnumerable<FreightRequestCheck>> GetAllFreightRequestChecksAsync()
    {
        return await freightRequestCheckRepository.GetAll();
    }
    
    public async Task<FreightRequestCheck> UpdateFreightRequestCheckAsync(FreightRequestCheck freightRequestCheck)
    {
        return await freightRequestCheckRepository.Update(freightRequestCheck);
    }
    
    public async Task DeleteFreightRequestCheckAsync(FreightRequestCheck freightRequestCheck)
    {
        await freightRequestCheckRepository.Delete(freightRequestCheck);
    }
    
}