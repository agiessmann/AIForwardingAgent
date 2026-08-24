using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public interface IFreightRequestService
{
    Task<FreightRequest> CreateFreightRequestAsync(FreightRequest freightRequest);
    Task<FreightRequest?> GetFreightRequestByIdAsync(int id);
    Task<IEnumerable<FreightRequest>> GetAllFreightRequestsAsync();
    Task<FreightRequest> UpdateFreightRequestAsync(FreightRequest freightRequest);
    Task<bool> DeleteFreightRequestAsync(FreightRequest freightRequest);
}

public class FreightRequestService(IRepositoryService<FreightRequest> repositoryService) : IFreightRequestService
{
    // CRUD operations for FreightRequest entity
    public async Task<FreightRequest> CreateFreightRequestAsync(FreightRequest freightRequest)
    {
        return await repositoryService.Create(freightRequest);
    }
    
    public async Task<FreightRequest?> GetFreightRequestByIdAsync(int id)
    {
        return await repositoryService.GetById(id);
    }
    
    public async Task<IEnumerable<FreightRequest>> GetAllFreightRequestsAsync()
    {
        return await repositoryService.GetAll();
    }
    
    public async Task<FreightRequest> UpdateFreightRequestAsync(FreightRequest freightRequest)
    {
        return await repositoryService.Update(freightRequest);
    }
    
    public async Task<bool> DeleteFreightRequestAsync(FreightRequest freightRequest)
    {
        return  await repositoryService.Delete(freightRequest);
    }
    
    
}