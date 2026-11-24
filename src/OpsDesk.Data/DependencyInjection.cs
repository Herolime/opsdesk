using Microsoft.Extensions.DependencyInjection;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Data.Db;
using OpsDesk.Data.Repositories;

namespace OpsDesk.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddData(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connectionString));
        services.AddSingleton<TicketRepository>();
        services.AddSingleton<ITicketRepository>(sp => sp.GetRequiredService<TicketRepository>());
        services.AddSingleton<ITicketNumberGenerator>(sp => sp.GetRequiredService<TicketRepository>());
        services.AddSingleton<ITicketQueryRepository, TicketQueryRepository>();
        return services;
    }

    public static IServiceCollection AddData(this IServiceCollection services, Func<IServiceProvider, string> connectionStringFactory)
    {
        services.AddSingleton<IDbConnectionFactory>(sp => new DbConnectionFactory(connectionStringFactory(sp)));
        services.AddSingleton<TicketRepository>();
        services.AddSingleton<ITicketRepository>(sp => sp.GetRequiredService<TicketRepository>());
        services.AddSingleton<ITicketNumberGenerator>(sp => sp.GetRequiredService<TicketRepository>());
        services.AddSingleton<ITicketQueryRepository, TicketQueryRepository>();
        return services;
    }

    public static void InitializeSchema(IServiceProvider provider, bool seedDemoTickets = false)
    {
        var factory = provider.GetRequiredService<IDbConnectionFactory>();
        SchemaInitializer.Initialize(factory);
        SchemaInitializer.SeedStreamMappings(factory);
        if (seedDemoTickets)
            SchemaInitializer.SeedSampleTickets(factory);
    }
}
