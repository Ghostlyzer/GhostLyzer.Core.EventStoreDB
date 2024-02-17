using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GhostLyzer.Core.EventStoreDB
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEventStore(
            this IServiceCollection services,
            IConfiguration configuration,
            params Assembly[] assemblies)
        {
            var assembliesToScan = assemblies.Length > 0 ? assemblies : new[] { Assembly.GetEntryAssembly()! };

            return services
                .AddEventStoreDB(configuration)
                .AddProjections(assembliesToScan);
        }
    }
}
