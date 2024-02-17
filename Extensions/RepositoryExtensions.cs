using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Repository;
using GhostLyzer.Core.Exceptions;

namespace GhostLyzer.Core.EventStoreDB.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="IEventStoreDBRepository{T}"/> interface.
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Retrieves an entity by its ID.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="repository">The repository to use for the operation.</param>
        /// <param name="id">The ID of the entity.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The entity, or throws an exception if not found.</returns>
        public static async Task<T> Get<T>(
            this IEventStoreDBRepository<T> repository,
            long id,
            CancellationToken cancellationToken
        ) where T : class, IAggregateEventSourcing<long>
        {
            var entity = await repository.FindAsync(id, cancellationToken);

            return entity ?? throw AggregateNotFoundException.For<T>(id); 
        }

        /// <summary>
        /// Retrieves an entity by its ID, updates it, and then saves the changes.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="repository">The repository to use for the operation.</param>
        /// <param name="id">The ID of the entity.</param>
        /// <param name="action">The action to perform on the entity.</param>
        /// <param name="expectedVersion">The expected version of the entity.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The version of the entity after the update.</returns>
        public static async Task<ulong> GetAndUpdate<T>(
            this IEventStoreDBRepository<T> repository,
            long id,
            Action<T> action,
            long? expectedVersion = null,
            CancellationToken cancellationToken = default
        ) where T : class, IAggregateEventSourcing<long>
        {
            var entity = await repository.Get(id, cancellationToken);

            action(entity);

            return await repository.UpdateAsync(entity, expectedVersion, cancellationToken);
        }
    }
}
