using GhostLyzer.Core.EventStoreDB.Events;
using MediatR;

namespace GhostLyzer.Core.EventStoreDB.Projections
{
    /// <summary>
    /// Defines a mechanism for publishing projections of stream events.
    /// </summary>
    public interface IProjectionPublisher
    {
        /// <summary>
        /// Publishes a projection of a specific type of stream event.
        /// </summary>
        /// <typeparam name="T">The type of data associated with the stream event.</typeparam>
        /// <param name="streamEvent">The stream event to publish a projection of.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishAsync<T>(StreamEvent<T> streamEvent, CancellationToken cancellationToken = default)
            where T : INotification;

        /// <summary>
        /// Publishes a projection of a stream event.
        /// </summary>
        /// <param name="streamEvent">The stream event to publish a projection of.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishAsync(StreamEvent streamEvent, CancellationToken cancellationToken = default);
    }
}
