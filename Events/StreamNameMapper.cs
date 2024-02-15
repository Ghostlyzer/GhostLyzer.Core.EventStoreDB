using System.Collections.Concurrent;

namespace GhostLyzer.Core.EventStoreDB.Events
{
    /// <summary>
    /// Provides a mapping between stream types and their names.
    /// </summary>
    public class StreamNameMapper
    {
        private static readonly StreamNameMapper Instance = new();

        private readonly ConcurrentDictionary<Type, string> TypeNameMap = new();

        /// <summary>
        /// Adds a custom mapping between a stream type and a name.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="mappedStreamName">The name to map to the stream type.</param>
        public static void AddCustomMap<TStream>(string mappedStreamName) =>
            AddCustomMap(typeof(TStream), mappedStreamName);

        /// <summary>
        /// Adds a custom mapping between a stream <see cref="Type"/> and a name.
        /// </summary>
        /// <param name="streamType">The type of the stream.</param>
        /// <param name="mappedStreamName">The name to map to the stream type.</param>
        public static void AddCustomMap(Type streamType, string mappedStreamName)
        {
            Instance.TypeNameMap.AddOrUpdate(streamType, mappedStreamName, (_, _) => mappedStreamName);
        }

        /// <summary>
        /// Converts a stream <see cref="Type"/> and an aggregate ID to a stream ID.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="aggregateId">The ID of the aggregate.</param>
        /// <param name="tenantId">The ID of the tenant, or null if there is no tenant.</param>
        /// <returns>The stream ID.</returns>
        public static string ToStreamId<TStream>(object aggregateId, object? tenantId = null) =>
            ToStreamId(typeof(TStream), aggregateId);

        /// <summary>
        /// Converts a stream <see cref="Type"/> and an aggregate ID to a stream ID.
        /// </summary>
        /// <param name="streamType">The type of the stream.</param>
        /// <param name="aggregateId">The ID of the aggregate.</param>
        /// <param name="tenantId">The ID of the tenant, or null if there is no tenant.</param>
        /// <returns>The stream ID.</returns>
        public static string ToStreamId(Type streamType, object aggregateId, object? tenantId = null)
        {
            var tenantPrefix = tenantId != null ? $"{tenantId}_" : "";

            return $"{tenantPrefix}{streamType.Name}-{aggregateId}";
        }
    }
}
