using System.Collections.Concurrent;
using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Enums;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Services;
using AiForwardingAgent.EmailService;
using AiForwardingAgent.ForwardingSkills;
using AiForwardingAgent.WorkflowExecutors;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.Services;

public sealed class AiAssistantSession : IAsyncDisposable
{
    private readonly IEmailMailboxService _emailMailboxService;
    private readonly IFreightRequestMissingInformationAgentService _freightRequestMissingInformationAgentService;
    private readonly IFreightRequestMissingInformationReplyAgentService _freightRequestMissingInformationReplyAgentService;
    private readonly IFreightRequestAgentService _freightRequestAgentService;
    private readonly IFreightRequestAnalyzeAgentService _freightRequestAnalyzeAgentService;
    private readonly IFreightRequestService _freightRequestService;
    private readonly IFreightLocationService _freightLocationService;
    private readonly IFreightRequirementMatrixService _freightRequirementMatrixService;
    private readonly IFreightRequestCheckService _freightRequestCheckService;
    private readonly IFreightRequestReplyAnalysisAgent _freightRequestReplyAnalysisAgentService;
    private readonly IFreightRequestUpdateService _freightRequestUpdateService;

    private readonly ConcurrentQueue<string> _messages = new();
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private CancellationTokenSource? _cts;
    private Task? _workerTask;

    public event Action? MessagesChanged;

    
    public AiAssistantSession(IEmailMailboxService emailMailboxService, 
        IFreightRequestMissingInformationAgentService freightRequestMissingInformationAgentService,
        IFreightRequestMissingInformationReplyAgentService freightRequestMissingInformationReplyAgentService, 
        IFreightRequestAgentService freightRequestAgentService, 
        IFreightRequestAnalyzeAgentService freightRequestAnalyzeAgentService, 
        IFreightRequestService freightRequestService, 
        IFreightLocationService freightLocationService, 
        IFreightRequirementMatrixService freightRequirementMatrixService, 
        IFreightRequestCheckService freightRequestCheckService, 
        IFreightRequestReplyAnalysisAgent freightRequestReplyAnalysisAgentService,
        IFreightRequestUpdateService freightRequestUpdateService)
    {
        _emailMailboxService = emailMailboxService;
        _freightRequestMissingInformationAgentService = freightRequestMissingInformationAgentService;
        _freightRequestMissingInformationReplyAgentService = freightRequestMissingInformationReplyAgentService;
        _freightRequestAgentService = freightRequestAgentService;
        _freightRequestAnalyzeAgentService = freightRequestAnalyzeAgentService;
        _freightRequestService = freightRequestService;
        _freightLocationService = freightLocationService;
        _freightRequirementMatrixService = freightRequirementMatrixService;
        _freightRequestCheckService = freightRequestCheckService;
        _freightRequestReplyAnalysisAgentService = freightRequestReplyAnalysisAgentService;
        _freightRequestUpdateService = freightRequestUpdateService;
    }

    public bool IsRunning => _workerTask is { IsCompleted: false };

    public IReadOnlyList<string> GetMessages() => _messages.ToList();

    public async Task StartAsync()
    {
        await _lifecycleLock.WaitAsync();
        try
        {
            if (IsRunning)
            {
                AddMessage("AI Assistant läuft bereits.");
                return;
            }

            _cts = new CancellationTokenSource();
            _workerTask = RunAsync(_cts.Token);

            AddMessage("AI Assistant wurde gestartet.");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifecycleLock.WaitAsync();
        try
        {
            if (_cts is null || _workerTask is null)
            {
                AddMessage("AI Assistant ist nicht aktiv.");
                return;
            }

            _cts.Cancel();
        }
        finally
        {
            _lifecycleLock.Release();
        }

        try
        {
            await _workerTask;
        }
        catch (OperationCanceledException)
        {
        }

        await _lifecycleLock.WaitAsync();
        try
        {
            _cts.Dispose();
            _cts = null;
            _workerTask = null;

            AddMessage("AI Assistant wurde gestoppt.");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public void ClearMessages()
    {
        while (_messages.TryDequeue(out _))
        {
        }

        MessagesChanged?.Invoke();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        
        try
        {
            AddMessage("Initialisiere AI Assistant Session...");

            while (!cancellationToken.IsCancellationRequested)
            {
                // Load and process messages from the AI assistant here.
                // Load messages from E - Mail Inbox here.
                var messages = await _emailMailboxService.ReadInboxAsync(10, true, cancellationToken);
                
                var unreadMessages = messages.Where(m => !m.IsRead).ToList();
                var messageCount = unreadMessages.Count;
                AddMessage($"AI Assistant Session läuft. Anzahl der ungelesenen E-Mails: {messageCount}");
                if (messageCount > 0)
                {
                    foreach (var message in unreadMessages)
                    {
                        AddMessage($"Neue E-Mail: Betreff: {message.Subject}, Von: {message.From}, Datum: {message.ReceivedAt}");
                        // Lade E-Mail-Inhalt und verarbeite ihn hier, z. B. durch Aufruf des AI-Agenten.
                        await RunWorkflowForSelectedMessageAsync(message, cancellationToken);
                    }   
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            AddMessage("AI Assistant Session wurde abgebrochen.");
        }
        catch (Exception ex)
        {
            AddMessage($"Fehler im AI Assistant: {ex.Message}");
        }
    }

    public ManualReviewRequest? PendingReview => _pendingReview?.Request;

    public event Action? ReviewChanged;

    private PendingReviewState? _pendingReview;

    private async Task<ManualReviewDecision> AskReviewerAsync(ManualReviewRequest request, CancellationToken cancellationToken)
    {
        var pending = new PendingReviewState(request);
        _pendingReview = pending;
        AddMessage($"Manuelle Prüfung erforderlich für Anfrage {request.FreightRequest.FreightRequestId}: {request.Reason}");
        ReviewChanged?.Invoke();

        try
        {
            await using var registration = cancellationToken.Register(() => pending.Completion.TrySetCanceled(cancellationToken));
            return await pending.Completion.Task;
        }
        finally
        {
            _pendingReview = null;
            ReviewChanged?.Invoke();
        }
    }

    public void CompleteReview(bool approved, string? comment = null)
        => _pendingReview?.Completion.TrySetResult(new ManualReviewDecision(approved, comment));

    private sealed record ManualReviewDecision(bool Approved, string? Comment);

    private sealed class PendingReviewState(ManualReviewRequest request)
    {
        public ManualReviewRequest Request { get; } = request;

        public TaskCompletionSource<ManualReviewDecision> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void AddMessage(string message)
    {
        _messages.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");

        while (_messages.Count > 500 && _messages.TryDequeue(out _))
        {
        }

        MessagesChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (IsRunning)
        {
            await StopAsync();
        }

        _lifecycleLock.Dispose();
    }
    
    private async Task RunWorkflowForSelectedMessageAsync(MailboxEmailMessage message, CancellationToken cancellationToken)
    {
        // Implement the workflow logic for the selected message here.
        // For example, you can read the message content and process it with the AI agent.
        var messageContent = await _emailMailboxService.ReadMessageContentAsync(message.Id, cancellationToken);
        AddMessage($"Verarbeite E-Mail mit ID: {message.Id}. Inhalt: {messageContent}");



        var freightRequest = new FreightRequestDto();
        freightRequest.FreightRequestEmailHistory = new List<EmailDto>
        {
            new EmailDto
            {
                Subject = message.Subject,
                Body = messageContent.BodyText,
                MessageId = int.Parse(message.Id),
                EmailSender = message.From,
                EmailType = FreightRequestEmailType.OriginalEmail,
                SentReceivedDate = message.ReceivedAt,
                Direction = EmailDirection.Incoming
            }
        };
        
        
        // Create Executors for the AI agent workflow and execute them as needed.
        var freightRequestMissingInformationDetectionExecutor = new FreightRequestMissingInformationDetectionExecutor(_freightRequestMissingInformationReplyAgentService);
        var freightRequestDetectionExecutor = new FreightRequestDetectionExecutor(_freightRequestAgentService);
        var freightRequestAnalyzerExecutor = new FreightRequestAnalyzeExecutor(_freightRequestAnalyzeAgentService);
        var confirmationExecutor = new ConfirmFreightRequestEmailExecutor(_emailMailboxService);
        var createFreightRequestExecutor = new CreateFreightRequestToDbExecutor(_freightRequestService, _freightRequirementMatrixService);
        var createFreightRequestCheckExecutor = new CreateFreightRequestCheckToDb(_freightRequestCheckService);
        var createMissingInfomationEmailExecutor = new CreateFreightRequestMissingInfomationEmailExecutor(_emailMailboxService,
            _freightRequestMissingInformationAgentService,
            _freightRequirementMatrixService,
            _freightRequestService);
        var freightRequestAnalyzeMissingInformationExecutor = new FreightRequestAnalyzeMissingInformationExecutor(_freightRequestReplyAnalysisAgentService,
            _freightRequestUpdateService);
        
        var persistReplyExecutor = new CreateFreightRequestToDbExecutor(_freightRequestService, _freightRequirementMatrixService, "PersistReplyToDbExecutor");
        var replyMissingInformationEmailExecutor = new CreateFreightRequestMissingInfomationEmailExecutor(_emailMailboxService,
            _freightRequestMissingInformationAgentService,
            _freightRequirementMatrixService,
            _freightRequestService,
            "ReplyMissingInformationEmailExecutor");
        var reviewRequestExecutor = new ManualReviewRequestExecutor();
        var reviewPort = RequestPort.Create<ManualReviewRequest, ManualReviewResponse>("ManualReviewPort");
        var reviewDecisionExecutor = new ManualReviewDecisionExecutor(_freightRequestService);
        
        // Build the workflow with conditional edges
        var workflow = new WorkflowBuilder(freightRequestMissingInformationDetectionExecutor)
            .AddEdge(freightRequestMissingInformationDetectionExecutor, freightRequestAnalyzeMissingInformationExecutor, GetConditionIsFreightRequestMissingInfo(expectedResult: true)) // If it's a freight request missing information response, go to confirmation
            .AddEdge(freightRequestAnalyzeMissingInformationExecutor, persistReplyExecutor) // Updates are always persisted before the review decision
            .AddEdge<FreightRequestDto>(persistReplyExecutor, confirmationExecutor, x => x is { RequiresManualReview: false } && IsProcessable(x))
            .AddEdge<FreightRequestDto>(persistReplyExecutor, replyMissingInformationEmailExecutor, x => x is { RequiresManualReview: false } && !IsProcessable(x))
            .AddEdge<FreightRequestDto>(persistReplyExecutor, reviewRequestExecutor, x => x is { RequiresManualReview: true })
            .AddEdge(reviewRequestExecutor, reviewPort)
            .AddEdge(reviewPort, reviewDecisionExecutor)
            .AddEdge<FreightRequestDto>(reviewDecisionExecutor, confirmationExecutor, x => x is { RequiresManualReview: false } && IsProcessable(x))
            .AddEdge<FreightRequestDto>(reviewDecisionExecutor, replyMissingInformationEmailExecutor, x => x is { RequiresManualReview: false } && !IsProcessable(x))
            .AddEdge(freightRequestMissingInformationDetectionExecutor, freightRequestDetectionExecutor, GetConditionIsFreightRequestMissingInfo(expectedResult: false)) // If it's not a freight request missing information response, go to detection
            .AddEdge(freightRequestDetectionExecutor ,createFreightRequestCheckExecutor, GetConditionIsFreightRequest(expectedResult: true)) // If it's a freight request, go to analyze
            .AddEdge(createFreightRequestCheckExecutor ,freightRequestAnalyzerExecutor) 
            .AddEdge(freightRequestAnalyzerExecutor, createFreightRequestExecutor)
            .AddEdge(createFreightRequestExecutor, createMissingInfomationEmailExecutor)
            .AddEdge(freightRequestDetectionExecutor ,createFreightRequestCheckExecutor, GetConditionIsFreightRequest(expectedResult: false)) // If it's not freight request, go and save check result
            .Build();

        
        // Run the workflow for the selected message
        var run = await InProcessExecution.RunStreamingAsync(workflow, freightRequest , cancellationToken: cancellationToken);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (var evt in run.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            if (evt is WorkflowOutputEvent outputEvent)
            {
                AddMessage($"{outputEvent}");
            }
            switch (evt)
            {
                case ExecutorInvokedEvent invoke:
                    AddMessage($"Starting {invoke.ExecutorId}");
                    break;

                case ExecutorCompletedEvent complete:
                    AddMessage($"Completed {complete.ExecutorId}: {complete.Data}");
                    break;

                case RequestInfoEvent requestInfo:
                    if (!requestInfo.Request.TryGetDataAs<ManualReviewRequest>(out var review))
                        break;

                    var decision = await AskReviewerAsync(review, cancellationToken);
                    await run.SendResponseAsync(requestInfo.Request.CreateResponse(
                        new ManualReviewResponse(review.FreightRequest, decision.Approved, decision.Comment)));
                    break;

                case WorkflowOutputEvent output:
                    AddMessage($"Workflow output: {output.Data}");
                    return;

                case WorkflowErrorEvent error:
                    AddMessage($"Workflow error: {error.Exception}");
                    return;
            }
        }
    }
    
    private static bool IsProcessable(FreightRequestDto? request) =>
        request?.MissingInformation?.All(x => x.Level != RequirementLevel.Required) ?? true;

    /// <summary>
    /// Creates a condition for messages based on the expected freight request detection result.
    /// </summary>
    /// <param name="expectedResult">The expected freight request detection result</param>
    /// <returns>A function that evaluates whether a message meets the expected result</returns>
    private static Func<object?,bool> GetConditionIsFreightRequest(bool expectedResult) =>
        detectionResult => detectionResult is FreightRequestDto result && result.IsFreightRequest == expectedResult;
    
    /// <summary>
    /// Creates a condition for messages based on the expected freight request missing information detection result.
    /// </summary>
    /// <param name="expectedResult">The expected missing information result</param>
    private static Func<object?,bool> GetConditionIsFreightRequestMissingInfo(bool expectedResult) =>
        detectionResult => detectionResult is FreightRequestDto result && result.FreightRequestId > 0 == expectedResult;

}