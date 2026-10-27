using System.Collections.Concurrent;
using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Models;
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
        IFreightRequestCheckService freightRequestCheckService)
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
        


        var freightRequest = new FreightRequestDto()
        {
            FreightRequestEmail = new EmailDto
            {
                Subject = message.Subject,
                Body = messageContent.BodyText,
                MessageId = int.Parse(message.Id),
                EmailSender = message.From
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
        
        // Build the workflow with conditional edges
        var workflow = new WorkflowBuilder(freightRequestMissingInformationDetectionExecutor)
            .AddEdge(freightRequestMissingInformationDetectionExecutor, confirmationExecutor, GetConditionIsFreightRequestMissingInfo(expectedResult: true)) // If it's a freight request missing information response, go to confirmation
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

                case WorkflowOutputEvent output:
                    AddMessage($"Workflow output: {output.Data}");
                    return;

                case WorkflowErrorEvent error:
                    AddMessage($"Workflow error: {error.Exception}");
                    return;
            }
        }
    }
    
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