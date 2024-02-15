using EventStore.Client;
using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Extensions;
using Newtonsoft.Json;
using System.Text;

namespace GhostLyzer.Core.EventStoreDB.Serialization
{
    /// <summary>
    /// Provides methods for serializing and deserializing events to and from EventStoreDB.
    /// </summary>
    public static class EventStoreDBSerializer
    {
        private static readonly JsonSerializerSettings _serializerSettings =
            new JsonSerializerSettings().WithNonDefaultConstructorContractResolver();

        /// <summary>
        /// Deserializes the data of a resolved event to a specific type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the event data to.</typeparam>
        /// <param name="resolvedEvent">The resolved event to deserialize.</param>
        /// <returns>The deserialized event data, or null if the event type is not known.</returns>
        public static T? Deserialize<T>(this ResolvedEvent resolvedEvent) where T : class => Deserialize(resolvedEvent) as T;

        /// <summary>
        /// Deserializes the data of a resolved event.
        /// </summary>
        /// <param name="resolvedEvent">The resolved event to deserialize.</param>
        /// <returns>The deserialized event data, or null if the event type is not known.</returns>
        public static object? Deserialize(this ResolvedEvent resolvedEvent)
        {
            var eventType = EventTypeMapper.ToType(resolvedEvent.Event.EventType);

            if (eventType == null) return null;

            return JsonConvert.DeserializeObject(
                Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                eventType,
                _serializerSettings);
        }

        /// <summary>
        /// Serializes an event to JSON and creates an <see cref="EventData"/> instance.
        /// </summary>
        /// <param name="event">The event to serialize.</param>
        /// <returns>An <see cref="EventData"/> instance containing the serialized event.</returns>
        public static EventData ToJsonEventData(this object @event) =>
            new(
                Uuid.NewUuid(),
                EventTypeMapper.ToName(@event.GetType()),
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)),
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { }))
            );
    }
}
