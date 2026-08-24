using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public partial class CustomerService(IRepositoryService<Customer> customerRepository, ILogger<CustomerService> logger) : ICustomerService
{
    // Implement methods for managing customers, e.g., GetAllCustomers, GetCustomerById, AddCustomer, UpdateCustomer, DeleteCustomer

    public async Task<IEnumerable<Customer>> GetAllCustomers()
    {
        try
        {
            return await customerRepository.GetAll();
        }
        catch (Exception ex)
        {
            LogErrorRetrievingAllCustomers(ex);
            throw;
        }
    }
    
    public async Task<Customer?> GetCustomerById(int customerId)
    {
        try
        {
            return await customerRepository.GetById(customerId);
        }
        catch (Exception ex)
        {
            LogErrorRetrievingCustomerById(ex, customerId);
            throw;
        }
    }
    
    public async Task<Customer> AddCustomer(Customer customer)
    {
        return await customerRepository.Create(customer);
    }
    
    public  async Task<Customer> UpdateCustomer(Customer customer)
    {
        return await customerRepository.Update(customer);
    }
    
    public async Task<bool> DeleteCustomer(Customer customer)
    {
        return await customerRepository.Delete(customer);
    }
    
    

    private void LogErrorRetrievingCustomerById(Exception exception, int customerId)
    {
        throw new NotImplementedException();
    }

    [LoggerMessage(LogLevel.Error, "Error retrieving all customers.")]
    partial void LogErrorRetrievingAllCustomers(Exception exception);
}