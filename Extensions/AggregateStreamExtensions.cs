using EventStore.Client;
using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Serialization;

namespace GhostLyzer.Core.EventStoreDB.Extensions
{
    public static class AggregateStreamExtensions
    {
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
