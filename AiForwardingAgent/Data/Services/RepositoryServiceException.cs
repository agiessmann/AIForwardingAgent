namespace AiForwardingAgent.Data.Services;

public class RepositoryServiceException : Exception
{
    public RepositoryServiceException()
    {
    }

    public RepositoryServiceException(string? message) : base(message)
    {
    }

    public RepositoryServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}