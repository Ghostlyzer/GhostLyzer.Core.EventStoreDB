using EventStore.Client;
using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Extensions;
using GhostLyzer.Core.EventStoreDB.Serialization;

namespace GhostLyzer.Core.EventStoreDB.Repository
{
    /// <summary>
    /// Provides a repository for interacting with EventStoreDB.
    /// </summary>
    /// <typeparam name="T">The type of the aggregate that the repository works with. The type must implement IAggregateEventSourcing&lt;long&gt;.</typeparam>
    public class EventStoreDBRepository<T> : IEventStoreDBRepository<T> where T : class, IAggregateEventSourcing<long>
    {
        private readonly EventStoreClient _eventStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDBRepository&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="eventStore">The EventStore client to use for interacting with EventStoreDB.</param>
        public EventStoreDBRepository(EventStoreClient eventStore)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        /// <summary>
        /// Adds a new aggregate to the EventStoreDB.
        /// </summary>
        /// <param name="aggregate">The aggregate to add.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the revision number of the added aggregate.</returns>
        public async Task<ulong> AddAsync(T aggregate, CancellationToken cancellationToken)
        {
            var result = await _eventStore.AppendToStreamAsync(
                StreamNameMapper.ToStreamId<T>(aggregate.Id),
                StreamState.NoStream,
                GetEventsToStore(aggregate),
                cancellationToken: cancellationToken);

            return result.NextExpectedStreamRevision;
        }

        /// <summary>
        /// Deletes an existing aggregate from the EventStoreDB.
        /// </summary>
        /// <param name="aggregate">The aggregate to delete.</param>
        /// <param name="expectedRevision">The expected revision number of the aggregate.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the revision number of the deleted aggregate.</returns>
        public Task<ulong> DeleteAsync(T aggregate, long? expectedRevision = null, CancellationToken cancellationToken = default) =>
            UpdateAsync(aggregate, expectedRevision, cancellationToken);

        /// <summary>
        /// Finds an aggregate by its ID.
        /// </summary>
        /// <param name="id">The ID of the aggregate to find.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the found aggregate, or null if no aggregate was found with the provided ID.</returns
        public Task<T?> FindAsync(long id, CancellationToken cancellationToken) =>
            _eventStore.AggregateStream<T>(id, cancellationToken);

        /// <summary>
        /// Updates an existing aggregate in the EventStoreDB.
        /// </summary>
        /// <param name="aggregate">The aggregate to update.</param>
        /// <param name="expectedRevision">The expected revision number of the aggregate.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the revision number of the updated aggregate.</returns>
        public async Task<ulong> UpdateAsync(T aggregate, long? expectedRevision = null, CancellationToken cancellationToken = default)
        {
            var nextVersion = expectedRevision ?? aggregate.Version;

            var result = await _eventStore.AppendToStreamAsync(
                StreamNameMapper.ToStreamId<T>(aggregate.Id),
                (ulong)nextVersion,
                GetEventsToStore(aggregate),
                cancellationToken: cancellationToken);

            return result.NextExpectedStreamRevision;
        }

        /// <summary>
        /// Gets the events to store for a specific aggregate.
        /// </summary>
        /// <param name="aggregate">The aggregate to get the events for.</param>
        /// <returns>A collection of <see cref="EventData"/> objects representing the events to store.</returns
        private static IEnumerable<EventData> GetEventsToStore(T aggregate)
        {
            var events = aggregate.ClearDomainEvents();

            return events.Select(EventStoreDBSerializer.ToJsonEventData);
        }
    }
}
