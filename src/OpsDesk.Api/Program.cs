using System.Text.Json.Serialization;
using OpsDesk.Api.Middleware;
using OpsDesk.Application;
using OpsDesk.Infrastructure;
using OpsDesk.Infrastructure.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddApplication();
builder.Services.AddOpsDeskInfrastructure(sp =>
    sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
    ?? "Data Source=opsdesk.db");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seedDemo = !app.Environment.IsEnvironment("Testing");
    OpsDesk.Infrastructure.DependencyInjection.InitializeSchema(scope.ServiceProvider, seedDemoTickets: seedDemo);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program { }
