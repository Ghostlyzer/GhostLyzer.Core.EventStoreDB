using GhostLyzer.Core.Utils;
using System.Collections.Concurrent;

namespace GhostLyzer.Core.EventStoreDB.Events
{
    /// <summary>
    /// Provides a mapping between event types and their names.
    /// </summary>
    public class EventTypeMapper
    {
        private static readonly EventTypeMapper Instance = new();
        private readonly ConcurrentDictionary<string, Type?> typeMap = new();
        private readonly ConcurrentDictionary<Type, string> typeNameMap = new();

        /// <summary>
        /// Adds a custom mapping between an event type and a name.
        /// </summary>
        /// <typeparam name="T">The type of the event.</typeparam>
        /// <param name="mappedEventTypeName">The name to map to the event type.</param>
        public static void AddCustomMap<T>(string mappedEventTypeName) => AddCustomMap(typeof(T), mappedEventTypeName);

        /// <summary>
        /// Adds a custom mapping between an event type and a name.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="mappedEventTypeName">The name to map to the event type.</param>
        public static void AddCustomMap(Type eventType, string mappedEventTypeName)
        {
            Instance.typeNameMap.AddOrUpdate(eventType, mappedEventTypeName, (_, _) => mappedEventTypeName);
            Instance.typeMap.AddOrUpdate(mappedEventTypeName, eventType, (_, _) => eventType);
        }

        /// <summary>
        /// Converts an event type to a name.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <returns>The name of the event type.</returns>
        public static string ToName<TEventType>() => ToName(typeof(TEventType));

        /// <summary>
        /// Converts an event type to a name.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <returns>The name of the event type.</returns>
        public static string ToName(Type eventType) => Instance.typeNameMap.GetOrAdd(eventType, _ =>
        {
            var eventTypeName = eventType.FullName!.Replace(".", "_");

            Instance.typeMap.AddOrUpdate(eventTypeName, eventType, (_, _) => eventType);

            return eventTypeName;
        });

        /// <summary>
        /// Converts a name to an event type.
        /// </summary>
        /// <param name="eventTypeName">The name of the event type.</param>
        /// <returns>The event type, or null if the name does not correspond to a known event type.</returns>
        public static Type? ToType(string eventTypeName) => Instance.typeMap.GetOrAdd(eventTypeName, _ =>
        {
            var type = TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly(eventTypeName.Replace("_", "."));

            if (type == null) return null;

            Instance.typeNameMap.AddOrUpdate(type, eventTypeName, (_, _) => eventTypeName);

            return type;
        });

        private static void UpdateMaps(Type type, string typeName)
        {
            Instance.typeNameMap.AddOrUpdate(type, typeName, (_, _) => typeName);
            Instance.typeMap.AddOrUpdate(typeName, type, (_, _) => type);
        }
    }
}
