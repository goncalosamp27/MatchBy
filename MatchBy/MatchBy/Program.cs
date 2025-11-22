using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MatchBy.Components;
using MatchBy.Components.Account;
using MatchBy.Data;
using MatchBy.Data.Seeders;
using MatchBy.Extensions;
using MatchBy.Models;
using Blazorise;
using Blazorise.FluentValidation;
using Blazorise.Tailwind;
using Blazorise.Icons.FontAwesome;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Common;
using MatchBy.Hubs;
using MatchBy.Services.Notifications;
using MatchBy.Services.BackgroundJobs;
using MatchBy.Services.ChatMessages;
using MatchBy.Services.Conversations;
using MatchBy.Services.Email;
using MatchBy.Services.FileValidator;
using MatchBy.Services.Friends;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Matches;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.PlayerRatings;
using MatchBy.Services.Teams;
using MatchBy.Services.TeamInvites;
using MatchBy.Services.Users;
using MatchBy.Settings;
using Resend;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using INotificationService = MatchBy.Services.Notifications.INotificationService;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ApplicationSeeder>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISeeder, UserSeeder>();
builder.Services.AddScoped<ISeeder, TeamSeeder>();
builder.Services.AddScoped<ISeeder, MatchSeeder>();
builder.Services.AddScoped<ISeeder, TeamInviteSeeder>();
builder.Services.AddScoped<ISeeder, MatchInviteSeeder>();
builder.Services.AddScoped<ISeeder, PlayerRatingSeeder>();
builder.Services.AddScoped<ISeeder, FriendSeeder>();
builder.Services.AddScoped<ISeeder, ConversationSeeder>();
builder.Services.AddScoped<ISeeder, ChatMessageSeeder>();
builder.Services.AddScoped<IFileValidator, FileValidator>();

builder.Services.AddAwsS3(builder.Configuration);
builder.Services.Configure<UploadSettings>(builder.Configuration.GetSection("UploadSettings"));


builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Resend:ApiKey"] ??
                 throw new InvalidOperationException("Resend ApiKey not found in configuration.");
});
builder.Services.AddTransient<IResend, ResendClient>();

/*builder.WebHost.UseSentry(options =>
{
    string? dsn = builder.Configuration["Sentry:DSN"];

    if (string.IsNullOrEmpty(dsn))
    {
        throw new InvalidOperationException("Sentry DSN not found in configuration.");
    }

    options.Dsn = dsn;
    options.TracesSampleRate = 1.0;
    options.Debug = true;
});*/

builder.Services.AddLocalTimeZoneServer();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ??
                                 throw new InvalidOperationException("Google ClientId not found in configuration.");
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ??
                                     throw new InvalidOperationException(
                                         "Google ClientSecret not found in configuration.");
    })
    .AddGitHub(githubOptions =>
    {
        githubOptions.ClientId = builder.Configuration["Authentication:Github:ClientId"] ??
                                 throw new InvalidOperationException("Github ClientId not found in configuration.");
        githubOptions.ClientSecret = builder.Configuration["Authentication:Github:ClientSecret"] ??
                                     throw new InvalidOperationException(
                                         "Github ClientSecret not found in configuration.");
    })
    .AddDiscord(discordOptions =>
    {
        discordOptions.ClientId = builder.Configuration["Authentication:Discord:ClientId"] ??
                                  throw new InvalidOperationException("Discord ClientId not found in configuration.");
        discordOptions.ClientSecret = builder.Configuration["Authentication:Discord:ClientSecret"] ??
                                      throw new InvalidOperationException(
                                          "Discord ClientSecret not found in configuration.");
    });

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                          throw new InvalidOperationException(
                              "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHangfire(config =>
    {
        config.UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString));
    }
);

builder.Services.AddHangfireServer();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAntiforgery();

builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;
        options.ProductToken = builder.Configuration["Blazorise:ProductToken"] ??
                               throw new InvalidOperationException(
                                   "Blazorise product token not found in configuration.");
    })
    .AddTailwindProviders()
    .AddFontAwesomeIcons()
    .AddBlazoriseFluentValidation();

builder.Services.AddValidatorsFromAssembly(typeof(App).Assembly);

builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IEmailSender<ApplicationUser>, EmailSender>();
builder.Services.AddScoped<IImageRefreshService, ImageRefreshService>();
builder.Services.AddScoped<IMatchesService, MatchesService>();
builder.Services.AddScoped<IMatchesInvitesService, MatchesInvitesService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ITeamsInvitesService, TeamsInvitesService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IPlayerRatingService, PlayerRatingService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ChatState>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("NewPolicy", corsPolicyBuilder =>
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    await app.RecreateDatabase();
    //await app.ApplyMigrationsAsync();
    await app.SeedDatabaseAsync();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

using (IServiceScope scope = app.Services.CreateScope())
{
    IRecurringJobManager recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate(
        "process-match-states",
        Job.FromExpression<IJobService>(service => service.ProcessMatchStatesAsync()),
        "*/1 * * * *" // Every minute
    );
}

app.UseHttpsRedirection();
app.UseCors("NewPolicy");
app.MapStaticAssets();
app.MapControllers();
app.UseStatusCodePagesWithReExecute("/error-page/{0}");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseHangfireDashboard();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();
app.MapAdditionalIdentityEndpoints();

await app.RunAsync();