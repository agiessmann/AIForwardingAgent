using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public interface IEMailSenderInformationService
{
    Task<EmailSenderInformation> CreateAsync(EmailSenderInformation emailSenderInformation);
    Task<IEnumerable<EmailSenderInformation>> GetAllAsync();
    Task<EmailSenderInformation?> GetByIdAsync(int emailSenderInformationId);
    Task<IEnumerable<EmailSenderInformation>> GetByCustomerIdAsync(int customerId);
    Task<EmailSenderInformation> UpdateAsync(EmailSenderInformation emailSenderInformation);
    Task<bool> DeleteAsync(int emailSenderInformationId);
    Task<EmailSenderInformation?> GetByEmailAddressAsync(string emailAddress);
}