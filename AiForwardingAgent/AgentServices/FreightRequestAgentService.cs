using Microsoft.Agents.AI;

namespace AiForwardingAgent.AgentServices;

public interface IFreightRequestAgentService
{
    Task<FreightRequestCheckResult> CheckIfFreightRequestAsync(
        string emailSubject,
        string emailText,
        string messageId,
        CancellationToken cancellationToken = default);
}

public class FreightRequestAgentService([FromKeyedServices("freight-request-check-agent")] AIAgent agent) : IFreightRequestAgentService
{
    
    // check if text is a freight request
    public async Task<FreightRequestCheckResult> CheckIfFreightRequestAsync(
        string emailSubject,
        string emailText,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildIsFreightRequestPrompt(emailSubject, emailText);
        var result = await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        var freightRequestCheckResult = FreightRequestCheckResult.FromJson(result.ToString());
        freightRequestCheckResult?.MessageId = messageId;
        freightRequestCheckResult?.Subject = emailSubject;
        freightRequestCheckResult?.EmailText = emailText;
        if (freightRequestCheckResult != null) return freightRequestCheckResult;
        return new FreightRequestCheckResult
        {
            IsFreightRequest = false,
            Confidence = 0.0m,
            Reason = "Failed to parse AI response.",
            MessageId = "0"
        };
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