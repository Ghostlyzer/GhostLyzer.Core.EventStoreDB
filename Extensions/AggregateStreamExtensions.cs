using EventStore.Client;
using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Serialization;

namespace GhostLyzer.Core.EventStoreDB.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="EventStoreClient"/> class.
    /// </summary>
    public static class AggregateStreamExtensions
    {
        /// <summary>
        /// Aggregates a stream of events into a projection.
        /// </summary>
        /// <typeparam name="T">The type of the projection.</typeparam>
        /// <param name="eventStore">The EventStore client to use for the operation.</param>
        /// <param name="id">The ID of the stream.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <param name="fromVersion">The version to start reading from, or null to start from the beginning.</param>
        /// <returns>The projection, or null if the stream was not found.</returns>
        public static async Task<T>? AggregateStream<T>(
            this EventStoreClient eventStore,
            long id,
            CancellationToken cancellationToken,
            ulong? fromVersion = null
            ) where T : class, IProjection
        {
            var readResult = eventStore.ReadStreamAsync(
                Direction.Forwards,
                StreamNameMapper.ToStreamId<T>(id),
                fromVersion ?? StreamPosition.Start,
                cancellationToken: cancellationToken
            );

            // TODO: consider adding extension method for the aggregation and deserialisation
            var aggregate = (T)Activator.CreateInstance(typeof(T), true)!;

            if (await readResult.ReadState == ReadState.StreamNotFound) return null;

            await foreach (var @event in readResult)
            {
                var eventData = @event.Deserialize();

                aggregate.When(eventData!);
            }

            return aggregate;
        }
    }
}
