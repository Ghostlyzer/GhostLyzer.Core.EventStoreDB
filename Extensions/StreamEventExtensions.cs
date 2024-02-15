using EventStore.Client;
using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Serialization;

namespace GhostLyzer.Core.EventStoreDB.Extensions
{
    /// <summary>
    /// Provides extension methods for working with <see cref="StreamEvent"/> instances.
    /// </summary>
    public static class StreamEventExtensions
    {
        /// <summary>
        /// Converts a <see cref="ResolvedEvent"/> instance to a <see cref="StreamEvent"/> instance.
        /// </summary>
        /// <param name="resolvedEvent">The <see cref="ResolvedEvent"/> instance to convert.</param>
        /// <returns>A <see cref="StreamEvent"/> instance that represents the same event as the <see cref="ResolvedEvent"/> instance, or null if the event data could not be deserialized.</returns>
        public static StreamEvent? ToStreamEvent(this ResolvedEvent resolvedEvent)
        {
            // Deserialize the event data from the resolved event
            // and check for null
            var eventData = resolvedEvent.Deserialize();
            if (eventData == null) return null;

            // Create a new EventMetadata instance using the event number and commit position from the resolved event
            var metaData = new EventMetadata(resolvedEvent.Event.EventNumber.ToUInt64(), resolvedEvent.Event.Position.CommitPosition);

            // Get the type of the generic StreamEvent class for the type of the event data
            var type = typeof(StreamEvent<>).MakeGenericType(eventData.GetType());

            // Create a new instance of the StreamEvent class for the type of the event data, and return it
            return (StreamEvent)Activator.CreateInstance(type, eventData, metaData)!;
        }
    }
}
