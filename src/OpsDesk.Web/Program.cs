using MudBlazor.Services;
using OpsDesk.Application;
using OpsDesk.Data;
using OpsDesk.Web.Components;
using OpsDesk.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=opsdesk.db";

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddApplication();
builder.Services.AddData(connectionString);
builder.Services.AddScoped<RoleState>();
builder.Services.AddScoped<NotificationState>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    OpsDesk.Data.DependencyInjection.InitializeSchema(scope.ServiceProvider, seedDemoTickets: true);
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
