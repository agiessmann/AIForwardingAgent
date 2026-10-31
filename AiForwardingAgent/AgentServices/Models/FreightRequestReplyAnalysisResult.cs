namespace AiForwardingAgent.AgentServices.Models;

public sealed class FreightRequestReplyAnalysisResult
{
    public bool IsRelevantReply { get; init; }

    public bool RequiresManualReview { get; init; }

    public List<ShipmentFieldUpdate> Updates { get; init; } = [];

    public List<AnsweredMissingInformation> AnsweredQuestions { get; init; } = [];

    public List<AnsweredMissingInformation> DeclinedQuestions { get; init; } = [];

    public List<UnresolvedInformation> UnresolvedInformation { get; init; } = [];

    public List<InformationConflict> Conflicts { get; init; } = [];

    public string? Reason { get; init; }
}