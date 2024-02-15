using GhostLyzer.Core.EventStoreDB.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GhostLyzer.Core.EventStoreDB.Projections
{
    /// <summary>
    /// Provides a mechanism for publishing projections of stream events.
    /// </summary>
    public class ProjectionPublisher : IProjectionPublisher
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionPublisher"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use for getting services.</param>
        public ProjectionPublisher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Publishes a projection of a specific type of stream event.
        /// </summary>
        /// <typeparam name="T">The type of data associated with the stream event.</typeparam>
        /// <param name="streamEvent">The stream event to publish a projection of.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task PublishAsync<T>(StreamEvent<T> streamEvent, CancellationToken cancellationToken = default) where T : INotification
        {
            using var scope = _serviceProvider.CreateScope();
            
            var projectionProcessors = scope.ServiceProvider.GetRequiredService<IEnumerable<IProjectionProcessor>>();

            foreach ( var projectionProcessor in projectionProcessors)
            {
                await projectionProcessor.ProcessEventAsync(streamEvent, cancellationToken);
            }
        }

        /// <summary>
        /// Publishes a projection of a stream event.
        /// </summary>
        /// <param name="streamEvent">The stream event to publish a projection of.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PublishAsync(StreamEvent streamEvent, CancellationToken cancellationToken = default)
        {
            var streamData = streamEvent.Data.GetType();

            var method = typeof(IProjectionPublisher)
                .GetMethods()
                .Single(method => method.Name == nameof(PublishAsync) && method.GetGenericArguments().Any())
                .MakeGenericMethod(streamData);

            return (Task)method
                .Invoke(this, new object[] { streamEvent, cancellationToken })!;
        }
    }
}
