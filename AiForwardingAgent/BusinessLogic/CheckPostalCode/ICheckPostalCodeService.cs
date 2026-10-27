namespace AiForwardingAgent.BusinessLogic.CheckPostalCode;

public interface ICheckPostalCodeService
{
    Task<CheckPostalCodeResult> CheckAsync(
        string postalCode,
        string city,
        CancellationToken cancellationToken = default);
}