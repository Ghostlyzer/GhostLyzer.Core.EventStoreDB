using GhostLyzer.Core.EventStoreDB.Events;

namespace GhostLyzer.Core.EventStoreDB.Repository
{
    /// <summary>
    /// Defines a repository for interacting with EventStoreDB.
    /// </summary>
    /// <typeparam name="T">The type of the aggregate that the repository works with. The type must implement IAggregateEventSourcing&lt;long&gt;.</typeparam>
    public interface IEventStoreDBRepository<T> where T : class, IAggregateEventSourcing<long>
    {
        /// <summary>
        /// Finds an aggregate by its ID.
        /// </summary>
        /// <param name="id">The ID of the aggregate to find.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the found aggregate, or null if no aggregate was found with the provided ID.</returns>
        Task<T?> FindAsync(long id, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a new aggregate to the EventStoreDB.
        /// </summary>
        /// <param name="aggregate">The aggregate to add.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the revision number of the added aggregate.</returns>
        Task<ulong> AddAsync(T aggregate, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing aggregate in the EventStoreDB.
        /// </summary>
        /// <param name="aggregate">The aggregate to update.</param>
        /// <param name="expectedRevision">The expected revision number of the aggregate.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the revision number of the updated aggregate.</returns>
        Task<ulong> UpdateAsync(T aggregate, long? expectedRevision = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an existing aggregate from the EventStoreDB.
        /// </summary>
        /// <param name="aggregate">The aggregate to delete.</param>
        /// <param name="expectedRevision">The expected revision number of the aggregate.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the revision number of the deleted aggregate.</returns>
        Task<ulong> DeleteAsync(T aggregate, long? expectedRevision = null, CancellationToken cancellationToken = default);
    }
}
