using Microsoft.Extensions.DependencyInjection;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Infrastructure.Email;
using OpsDesk.Infrastructure.Persistence.Db;
using OpsDesk.Infrastructure.Persistence.Repositories;
using OpsDesk.Infrastructure.Services;

namespace OpsDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOpsDeskInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionStringFactory)
    {
        services.AddSingleton<IDbConnectionFactory>(sp => new SqlConnectionFactory(connectionStringFactory(sp)));
        services.AddSingleton<IBusinessDaySlaCalculator, BusinessDaySlaCalculator>();
        services.AddSingleton<TicketRepository>();
        services.AddSingleton<ITicketRepository>(sp => sp.GetRequiredService<TicketRepository>());
        services.AddSingleton<ITicketNumberGenerator>(sp => sp.GetRequiredService<TicketRepository>());
        services.AddSingleton<ITicketQueryRepository, TicketQueryRepository>();
        services.AddSingleton<INotificationRepository, NotificationRepository>();
        services.AddSingleton<IEmailService, SmtpEmailService>();
        return services;
    }

    public static void InitializeSchema(IServiceProvider provider, bool seedDemoTickets = false)
    {
        var factory = provider.GetRequiredService<IDbConnectionFactory>();
        OpsDeskSchemaBaseline.Initialize(factory);
        OpsDeskSchemaBaseline.SeedStreamMappings(factory);
        if (seedDemoTickets)
            OpsDeskSchemaBaseline.SeedSampleTickets(factory);
    }
}
