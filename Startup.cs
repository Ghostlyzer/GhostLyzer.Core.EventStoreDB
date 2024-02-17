using EventStore.Client;
using GhostLyzer.Core.EventStoreDB.BackgroundWorkers;
using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Projections;
using GhostLyzer.Core.EventStoreDB.Repository;
using GhostLyzer.Core.EventStoreDB.Subscriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace GhostLyzer.Core.EventStoreDB
{
    public class EventStoreDbConfig
    {
        public string ConnectionString { get; set; } = default!;
    }

    public record EventStoreDBOptions(bool UseInternalCheckpointing = true);

    public static class EventStoreDbConfigExtensions
    {
        private const string DefaultConfigKey = "EventStore";

        public static IServiceCollection AddEventStoreDB(
            this IServiceCollection services,
            IConfiguration configuration,
            EventStoreDBOptions? options = null)
        {
            var eventStoreDBConfig = configuration.GetSection(DefaultConfigKey).Get<EventStoreDbConfig>();

            services
                .AddSingleton(new EventStoreClient(EventStoreClientSettings.Create(eventStoreDBConfig.ConnectionString)))
                .AddScoped(typeof(IEventStoreDBRepository<>), typeof(EventStoreDBRepository<>))
                .AddTransient<EventStoreDBSubscriptionToAll, EventStoreDBSubscriptionToAll>();

            if (options?.UseInternalCheckpointing != false)
                services.AddTransient<ISubscriptionCheckpointRepository, EventStoreDBSubscriptionCheckpointRepository>();

            return services;
        }

        public static IServiceCollection AddEventStoreDBSubscriptionToAll(
            this IServiceCollection services,
            EventStoreDBSubscriptionToAllOptions? subscriptionOptions = null,
            bool checkpointToEventStoreDB = true)
        {
            if (checkpointToEventStoreDB)
                services.AddTransient<ISubscriptionCheckpointRepository, EventStoreDBSubscriptionCheckpointRepository>();

            return services.AddHostedService(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<BackgroundWorker>>();

                var eventStoreDBSubscriptionToAll = serviceProvider.GetRequiredService<EventStoreDBSubscriptionToAll>();

                return new BackgroundWorker(
                    logger,
                    ct =>
                        eventStoreDBSubscriptionToAll.SubscribeToAll(
                            subscriptionOptions ?? new EventStoreDBSubscriptionToAllOptions(),
                            ct
                        )
                );
            });
        }

        public static IServiceCollection AddProjections(this IServiceCollection services, params Assembly[] assembliesToScan)
        {
            services.AddSingleton<IProjectionPublisher, ProjectionPublisher>();

            RegisterProjections(services, assembliesToScan);

            return services;
        }

        private static void RegisterProjections(IServiceCollection services, Assembly[] assembliesToScan)
        {
            services.Scan(scan => scan
                .FromAssemblies(assembliesToScan)
                .AddClasses(classes => classes.AssignableTo<IProjection>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());
        }
    }
}
