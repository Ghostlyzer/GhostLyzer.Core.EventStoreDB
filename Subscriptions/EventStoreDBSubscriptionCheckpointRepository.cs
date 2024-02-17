using EventStore.Client;
using GhostLyzer.Core.Domain.Event;
using GhostLyzer.Core.EventStoreDB.Serialization;

namespace GhostLyzer.Core.EventStoreDB.Subscriptions
{
    /// <summary>
    /// Represents a checkpoint that has been stored.
    /// </summary>
    public record CheckpointStored(string SubscriptionId, ulong? Position, DateTime CheckpointedAt) : IEvent;

    /// <summary>
    /// Represents a repository for managing subscription checkpoints in EventStoreDB.
    /// </summary>
    public class EventStoreDBSubscriptionCheckpointRepository : ISubscriptionCheckpointRepository
    {
        private readonly EventStoreClient _eventStoreClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDBSubscriptionCheckpointRepository"/> class.
        /// </summary>
        /// <param name="eventStoreClient">The EventStore client to use for the operation.</param>
        public EventStoreDBSubscriptionCheckpointRepository(EventStoreClient eventStoreClient)
        {
            _eventStoreClient = eventStoreClient ?? throw new ArgumentNullException(nameof(eventStoreClient));
        }

        /// <summary>
        /// Loads the checkpoint for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The checkpoint, or null if not found.</returns>
        public async ValueTask<ulong?> Load(string subscriptionId, CancellationToken cancellationToken)
        {
            var streamName = GetCheckpointStreamName(subscriptionId);

            var result = _eventStoreClient.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1, cancellationToken: cancellationToken);

            if(await result.ReadState == ReadState.StreamNotFound) return null;

            ResolvedEvent? @event = await result.FirstOrDefaultAsync(cancellationToken);

            return @event?.Deserialize<CheckpointStored>()?.Position;
        }

        /// <summary>
        /// Stores the checkpoint for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="position">The position of the checkpoint.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async ValueTask Store(string subscriptionId, ulong position, CancellationToken cancellationToken)
        {
            var @event = new CheckpointStored(subscriptionId, position, DateTime.UtcNow);
            var eventToAppend = new[] {@event.ToJsonEventData()};
            var streamName = GetCheckpointStreamName(subscriptionId);

            try
            {
                await _eventStoreClient.AppendToStreamAsync(
                    streamName,
                    StreamState.StreamExists,
                    eventToAppend,
                    cancellationToken: cancellationToken);
            }
            catch (WrongExpectedVersionException)
            {
                // If we encounter WrongExpectedVersionException it simply
                // means that stream does not exist.Now we have to set the
                // checkpoint stream to have at most 1 event using stream metadata $maxCount property
                await _eventStoreClient.SetStreamMetadataAsync(
                    streamName,
                    StreamState.NoStream,
                    new StreamMetadata(1),
                    cancellationToken: cancellationToken);

                // Now we can append the event again
                // but this time we expect that the stream does not exist
                await _eventStoreClient.AppendToStreamAsync(
                    streamName,
                    StreamState.NoStream,
                    eventToAppend,
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Gets the name of the checkpoint stream for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <returns>The name of the checkpoint stream.</returns>
        private static string GetCheckpointStreamName(string subscriptionId)
        {
            return $"checkpoint_{subscriptionId}";
        }
    }
}
