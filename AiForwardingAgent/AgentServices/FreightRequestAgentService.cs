using AiForwardingAgent.AgentServices.Models;
using Microsoft.Agents.AI;

namespace AiForwardingAgent.AgentServices;

public interface IFreightRequestAgentService
{
    Task<FreightRequestDto> CheckIfFreightRequestAsync(FreightRequestDto freightRequestDto,
        CancellationToken cancellationToken = default);
}

public class FreightRequestAgentService([FromKeyedServices("freight-request-check-agent")] AIAgent agent) : IFreightRequestAgentService
{
    
    // check if text is a freight request
    public async Task<FreightRequestDto> CheckIfFreightRequestAsync(
        FreightRequestDto freightRequestDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(freightRequestDto);

        var email = freightRequestDto.FreightRequestEmail
                    ?? throw new ArgumentNullException(nameof(freightRequestDto.FreightRequestEmail));

        if (string.IsNullOrWhiteSpace(email.Subject))
            throw new ArgumentException("Email subject cannot be null or empty.", nameof(freightRequestDto));

        if (string.IsNullOrWhiteSpace(email.Body))
            throw new ArgumentException("Email body cannot be null or empty.", nameof(freightRequestDto));

        var prompt = BuildIsFreightRequestPrompt(email.Subject, email.Body);

        var result = await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        var agentResponse = FreightRequestDto.FromJson(result.ToString());

        // Fallback: falls Agent kein parsebares JSON liefert, Input beibehalten
        if (agentResponse is null)
        {
            return freightRequestDto;
        }

        // Mail-Kontext aus Eingang behalten (Agent soll nur Klassifikation liefern)
        agentResponse.FreightRequestEmail = new EmailDto
        {
            Subject = email.Subject,
            Body = email.Body,
            MessageId = email.MessageId,
            EmailSender = email.EmailSender,
            EmailRecipient = email.EmailRecipient
        };

        // Optional: falls Agent CustomerReference nicht setzt, vorhandenen Wert übernehmen
        agentResponse.CustomerReference ??= freightRequestDto.CustomerReference;

        return agentResponse;
    }

    // prompts
    private static string BuildIsFreightRequestPrompt(string emailSubject, string emailText)
    {
    return $@"
        You are an AI assistant working for an international freight forwarding company.

        Determine whether the email is a NEW freight transportation quotation request.

        A freight request is an email where the sender requests:
        - a freight quotation
        - a transport quotation
        - a shipment
        - a pickup
        - a delivery
        - freight rates
        - booking of cargo
        - transport capacity
        - shipping services

        Typical transport modes:
        - Road Freight
        - Air Freight
        - Ocean Freight
        - Rail Freight
        - Courier / Express
        - Multimodal

        Examples of freight requests:
        - ""Please quote for the following shipment.""
        - ""Can you arrange transport from Hamburg to Milan?""
        - ""Please provide your best rate.""
        - ""We need a truck tomorrow.""
        - ""Request for quotation.""
        - ""RFQ""
        - ""Freight inquiry""

        The following are NOT freight requests:
        - shipment tracking
        - complaints
        - invoices
        - payment reminders
        - job applications
        - newsletters
        - marketing emails
        - internal discussions
        - customs documents without a transport request
        - replies that do not contain a new transport request

        Return this JSON structure:

        {{
          ""IsFreightRequest"": true,
          ""Confidence"": 0.97,
          ""Reason"": ""Customer requests a quotation for a shipment from Hamburg to Milan.""

        }}

        If the email is NOT a freight request, return:

        {{
          ""IsFreightRequest"": false,
          ""Confidence"": 0.99,
          ""Reason"": ""This email is only asking for shipment status.""
       }}

        Email subject:

        ------------------------
        {emailSubject}
        ------------------------

        Email body:

        ------------------------
        {emailText}
        ------------------------

        Return ONLY the JSON object.
        ";
    }
}