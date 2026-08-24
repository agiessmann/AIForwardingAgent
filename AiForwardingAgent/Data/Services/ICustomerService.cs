using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public interface ICustomerService
{
    Task<IEnumerable<Customer>> GetAllCustomers();
    Task<Customer?> GetCustomerById(int customerId);
    Task<Customer> AddCustomer(Customer customer);
    Task<Customer> UpdateCustomer(Customer customer);
    Task<bool> DeleteCustomer(Customer customer);
}