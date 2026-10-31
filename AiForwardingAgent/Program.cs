using AiForwardingAgent.AgentServices;
using AiForwardingAgent.BusinessLogic.CheckPostalCode;
using AiForwardingAgent.BusinessLogic.RouteLocationResolver;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using AiForwardingAgent.Components.Account;
using AiForwardingAgent.Components;
using AiForwardingAgent.Data;
using AiForwardingAgent.Data.Services;
using AiForwardingAgent.EmailService;
using AiForwardingAgent.ForwardingSkills;
using AiForwardingAgent.Services;
using Microsoft.Agents.AI.Hosting;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString), ServiceLifetime.Scoped);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// DI for Data Services
builder.Services.AddTransient(typeof(IRepositoryService<>), typeof(RepositoryService<>));
builder.Services.AddScoped<IEMailSenderInformationService, EMailSenderInformationService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IFreightRequestService, FreightRequestService>();
builder.Services.AddScoped<IFreightLocationService, FreightLocationService>();
builder.Services.AddScoped<IFreightPackageService, FreightPackageService>();
builder.Services.AddScoped<IFreightRequestCheckService, FreightRequestCheckService>();


// DI for E Mail Service
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<EMailMailboxService>(sp =>
    new EMailMailboxService(sp.GetRequiredService<IOptions<EmailOptions>>().Value));
builder.Services.AddSingleton<IEmailMailboxService>(sp => sp.GetRequiredService<EMailMailboxService>());
builder.Services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<EMailMailboxService>());
builder.Services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<EMailMailboxService>());

// DI for Skills / Business Logic
builder.Services.AddScoped<IFreightRequirementMatrixService, FreightRequirementMatrixService>();
//builder.Services.AddScoped<ICheckPostalCodeService, CheckPostalCodeService>();
builder.Services.AddHttpClient<ICheckPostalCodeService, CheckPostalCodeService>();
builder.Services.AddHttpClient<ILocationResolver, LocationResolver>();
    

// DI For Agents
// ChatClient registrieren

var apiKey = builder.Configuration["OPENAI_API_KEY"] ?? throw new InvalidOperationException("OPENAI_API_KEY fehlt.");

builder.Services.AddKeyedSingleton<IChatClient>(
    "gpt-5.4-mini",
    (_, _) => new ChatClient(
        model: "gpt-5.4-mini",
        apiKey: apiKey)
        .AsIChatClient());

builder.Services.AddKeyedSingleton<IChatClient>(
    "mistral",
    (_, _) => new OllamaChatClient(
        new Uri("http://localhost:11434"),
        "mistral"));

builder.Services.AddKeyedSingleton<IChatClient>(
    "qwen3.6",
    (_, _) => new OllamaChatClient(
        new Uri("http://localhost:11434"),
        "qwen3.6:35b"));

// Agenten registrieren
// allgemeiner chat support agent
builder.Services.AddAIAgent(
    name: "support-agent",
    instructions: "Du bist ein Support-Assistent.",
    chatClientServiceKey: "mistral");

// email customer extraction agent
builder.Services.AddAIAgent(
    name: "customer-extraction-agent",
    instructions: "You are a helpful assistant that extracts structured customer data from emails. Always return valid data.",
    chatClientServiceKey: "mistral");

// email chek if freight request agent
builder.Services.AddAIAgent(
    name: "freight-request-check-agent",
    instructions: "You are an AI assistant working for an international freight forwarding company. Determine whether the email is a NEW freight transportation request. Return ONLY the JSON object.",
    chatClientServiceKey: "gpt-5.4-mini");


// AgentServices
builder.Services.AddScoped<IFreightRequestAgentService, FreightRequestAgentService>();
builder.Services.AddScoped<IFreightRequestAnalyzeAgentService, FreightRequestAnalyzeAgentService>();
builder.Services.AddScoped<IFreightRequestMissingInformationAgentService, FreightRequestMissingInformationAgentService>();
builder.Services.AddScoped<IFreightRequestMissingInformationReplyAgentService, FreightRequestMissingInformationReplyAgentService>();
builder.Services.AddScoped<IFreightRequestReplyAnalysisAgent, FreightRequestReplyAnalysisAgent>();
builder.Services.AddScoped<IFreightRequestUpdateService, FreightRequestUpdateService>();

// AI Assistant Session
builder.Services.AddScoped<AiAssistantSession>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

/*if (app.Environment.IsDevelopment())
{
    var mailApi = app.MapGroup("/dev/mail");

    mailApi.MapPost("/read", async (MailReadRequest request, IEmailMailboxService mailboxService, CancellationToken cancellationToken) =>
    {
        var messages = await mailboxService.ReadInboxAsync(request.MaxCount, request.UnreadOnly, cancellationToken);
        return Results.Ok(messages);
    });

    mailApi.MapPost("/send", async (MailSendRequest request, IEmailMailboxService mailboxService, CancellationToken cancellationToken) =>
    {
        await mailboxService.SendEmailAsync(
            request.ToEmail,
            request.Subject,
            request.TextBody,
            request.HtmlBody,
            cancellationToken);

        return Results.Ok(new { sent = true });
    });
}*/

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();