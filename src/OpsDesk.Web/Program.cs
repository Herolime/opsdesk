using MudBlazor.Services;
using OpsDesk.Application;
using OpsDesk.Infrastructure;
using OpsDesk.Infrastructure.Email;
using OpsDesk.Web.Components;
using OpsDesk.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddApplication();
builder.Services.AddOpsDeskInfrastructure(sp =>
    sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
    ?? "Data Source=opsdesk.db");
builder.Services.AddScoped<RoleState>();
builder.Services.AddScoped<NotificationState>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    OpsDesk.Infrastructure.DependencyInjection.InitializeSchema(scope.ServiceProvider, seedDemoTickets: true);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
