using GhostLyzer.Core.Domain.Event;

namespace GhostLyzer.Core.EventStoreDB.Events
{
    /// <summary>
    /// Represents metadata for an event in a stream.
    /// </summary>
    public record EventMetadata(ulong StreamRevision, ulong LogPosition);

    /// <summary>
    /// Represents a generic event in a stream.
    /// </summary>
    public class StreamEvent : IEvent
    {
        /// <summary>
        /// Gets the data associated with the event.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Gets the metadata associated with the event.
        /// </summary>
        public EventMetadata Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamEvent"/> class.
        /// </summary>
        /// <param name="data">The data associated with the event.</param>
        /// <param name="metaData">The metadata associated with the event.</param>
        public StreamEvent(object data, EventMetadata metadata)
        {
            Data = data;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Represents a specific type of event in a stream.
    /// </summary>
    /// <typeparam name="T">The type of data associated with the event.</typeparam>
    public class StreamEvent<T> : StreamEvent where T : notnull
    {
        /// <summary>
        /// Gets the data associated with the event.
        /// </summary>
        public new T Data => (T)base.Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamEvent{T}"/> class.
        /// </summary>
        /// <param name="data">The data associated with the event.</param>
        /// <param name="metadata">The metadata associated with the event.</param>
        public StreamEvent(T data, EventMetadata metadata) : base(data, metadata) { }
    }
}
