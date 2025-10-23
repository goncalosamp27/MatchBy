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
using MatchBy.Services;
using Toolbelt.Blazor.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ApplicationSeeder>();
builder.Services.AddScoped<ISeeder, UserSeeder>();
builder.Services.AddScoped<ISeeder, TeamSeeder>();
builder.Services.AddScoped<ISeeder, MatchSeeder>();
builder.Services.AddScoped<ISeeder, TeamInviteSeeder>();
builder.Services.AddScoped<ISeeder, MatchInviteSeeder>();
builder.Services.AddScoped<ISeeder, TeamChatMessageSeeder>();
builder.Services.AddScoped<ISeeder, MatchChatMessageSeeder>();
builder.Services.AddScoped<ISeeder, PrivateChatMessageSeeder>();
builder.Services.AddScoped<ISeeder, PlayerRatingSeeder>();
builder.Services.AddScoped<ISeeder, FriendSeeder>();

builder.WebHost.UseSentry(options =>
{
    string? dsn = builder.Configuration["Sentry:DSN"];

    if (string.IsNullOrEmpty(dsn))
    {
        throw new InvalidOperationException("Sentry DSN not found in configuration.");
    }

    options.Dsn = dsn;
    options.TracesSampleRate = 1.0;
    options.Debug = true;
});

builder.Services.AddLocalTimeZoneServer();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                          throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services
    .AddBlazorise(options =>
    {
        options.ProductToken = builder.Configuration["Blazorise:ProductToken"] ?? throw new InvalidOperationException("Blazorise product token not found in configuration.");
    })
    .AddTailwindProviders()
    .AddFontAwesomeIcons()
    .AddBlazoriseFluentValidation();

builder.Services.AddValidatorsFromAssembly( typeof( App ).Assembly );

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<IMatchesService, MatchesService>();
builder.Services.AddScoped<IUsersService, UsersService>();

builder.Services.AddCors(options => 
{
    options.AddPolicy("NewPolicy", corsPolicyBuilder =>
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    await app.ApplyMigrationsAsync();
    await app.SeedDatabaseAsync();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("NewPolicy");
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MatchBy.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

await app.RunAsync();
