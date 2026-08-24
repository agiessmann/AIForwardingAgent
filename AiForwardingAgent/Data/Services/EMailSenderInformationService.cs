using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.Data.Services;

public class EMailSenderInformationService(
    IRepositoryService<EmailSenderInformation> repositoryService,
    ILogger<EMailSenderInformationService> logger) : IEMailSenderInformationService
{
    public async Task<EmailSenderInformation> CreateAsync(EmailSenderInformation emailSenderInformation)
    {
        ArgumentNullException.ThrowIfNull(emailSenderInformation);

        try
        {
            return await repositoryService.Create(emailSenderInformation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create EmailSenderInformation");
            throw;
        }
    }

    public async Task<IEnumerable<EmailSenderInformation>> GetAllAsync()
    {
        try
        {
            return await repositoryService.GetAll();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all EmailSenderInformation entries");
            throw;
        }
    }

    public async Task<EmailSenderInformation?> GetByIdAsync(int emailSenderInformationId)
    {
        try
        {
            return await repositoryService.GetByExpression(x => x.EmailSenderInformationId == emailSenderInformationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get EmailSenderInformation by id {EmailSenderInformationId}", emailSenderInformationId);
            throw;
        }
    }

    public async Task<IEnumerable<EmailSenderInformation>> GetByCustomerIdAsync(int customerId)
    {
        try
        {
            return await repositoryService.GetAllByExpression(x => x.CustomerId == customerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get EmailSenderInformation by customer id {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<EmailSenderInformation> UpdateAsync(EmailSenderInformation emailSenderInformation)
    {
        ArgumentNullException.ThrowIfNull(emailSenderInformation);

        try
        {
            return await repositoryService.Update(emailSenderInformation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update EmailSenderInformation with id {EmailSenderInformationId}", emailSenderInformation.EmailSenderInformationId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int emailSenderInformationId)
    {
        try
        {
            var emailSenderInformation = await repositoryService.GetByExpression(x => x.EmailSenderInformationId == emailSenderInformationId);
            if (emailSenderInformation is null)
            {
                return false;
            }

            return await repositoryService.Delete(emailSenderInformation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete EmailSenderInformation with id {EmailSenderInformationId}", emailSenderInformationId);
            throw;
        }
    }
    
    public async Task<EmailSenderInformation?> GetByEmailAddressAsync(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            logger.LogWarning("Email address cannot be null or empty");
            return null;
        }

        try
        {
            return await repositoryService.GetByExpression(x => x.EmailAddress == emailAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get EmailSenderInformation by email address {EmailAddress}", emailAddress);
            throw;
        }
    }
}