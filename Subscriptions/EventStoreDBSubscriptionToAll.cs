using EventStore.Client;
using GhostLyzer.Core.EventStoreDB.Events;
using GhostLyzer.Core.EventStoreDB.Extensions;
using GhostLyzer.Core.EventStoreDB.Projections;
using GhostLyzer.Core.Utils;
using Grpc.Core;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace GhostLyzer.Core.EventStoreDB.Subscriptions
{
    /// <summary>
    /// Represents options for a subscription to all events in EventStoreDB.
    /// </summary>
    public class EventStoreDBSubscriptionToAllOptions
    {
        /// <summary>
        /// Gets or sets the ID of the subscription. The default value is "default".
        /// </summary>
        public string SubscriptionId { get; set; } = "default";

        /// <summary>
        /// Gets or sets the filter options for the subscription. The default value excludes system events.
        /// </summary>
        public SubscriptionFilterOptions FilterOptions { get; set; } =
            new(EventTypeFilter.ExcludeSystemEvents());

        /// <summary>
        /// Gets or sets an action to configure the operation options for the subscription.
        /// </summary>
        public Action<EventStoreClientOperationOptions>? ConfigureOperation { get; set; }

        /// <summary>
        /// Gets or sets the user credentials for the subscription.
        /// </summary>
        public UserCredentials? UserCredentials { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to resolve link events for the subscription.
        /// </summary>
        public bool ResolveLinkTos { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore deserialization errors for the subscription. The default value is true.
        /// </summary>
        public bool IgnoreDeserializationErrors { get; set; } = true;
    }

    /// <summary>
    /// Represents a subscription to all events in EventStoreDB.
    /// </summary>
    public class EventStoreDBSubscriptionToAll
    {
        private readonly IProjectionPublisher _projectionPublisher;
        private readonly EventStoreClient _eventStoreClient;
        private readonly IMediator _mediator;
        private readonly ISubscriptionCheckpointRepository _checkpointRepository;
        private readonly ILogger<EventStoreDBSubscriptionToAll> _logger;
        private EventStoreDBSubscriptionToAllOptions _subscriptionOptions = default!;
        private string SubscriptionId => _subscriptionOptions.SubscriptionId;
        private readonly object resubscribeLock = new();
        private CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDBSubscriptionToAll"/> class.
        /// </summary>
        /// <param name="projectionPublisher">The projection publisher.</param>
        /// <param name="eventStoreClient">The EventStore client.</param>
        /// <param name="mediator">The mediator.</param>
        /// <param name="checkpointRepository">The checkpoint repository.</param>
        /// <param name="logger">The logger.</param>
        public EventStoreDBSubscriptionToAll(
            IProjectionPublisher projectionPublisher,
            EventStoreClient eventStoreClient,
            IMediator mediator,
            ISubscriptionCheckpointRepository checkpointRepository,
            ILogger<EventStoreDBSubscriptionToAll> logger)
        {
            _projectionPublisher = projectionPublisher;
            _eventStoreClient = eventStoreClient ?? throw new ArgumentNullException(nameof(eventStoreClient));
            _mediator = mediator;
            _checkpointRepository = checkpointRepository ?? throw new ArgumentNullException(nameof(checkpointRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Subscribes to all events in EventStoreDB.
        /// </summary>
        /// <param name="subscriptionOptions">The subscription options.</param>
        /// <param name="ct">The cancellation token.</param>
        public async Task SubscribeToAll(EventStoreDBSubscriptionToAllOptions subscriptionOptions, CancellationToken ct)
        {
            await Task.Yield();

            _subscriptionOptions = subscriptionOptions;
            cancellationToken = ct;

            _logger.LogInformation("Initiating subscription to all events with ID '{SubscriptionId}'", _subscriptionOptions.SubscriptionId);

            var checkpoint = await _checkpointRepository.Load(SubscriptionId, ct);

            await _eventStoreClient.SubscribeToAllAsync(
                checkpoint == null ? FromAll.Start : FromAll.After(new Position(checkpoint.Value, checkpoint.Value)),
                HandleEvent,
                _subscriptionOptions.ResolveLinkTos,
                HandleDrop,
                _subscriptionOptions.FilterOptions,
                _subscriptionOptions.UserCredentials,
                ct);

            _logger.LogInformation("Subscription to all events with ID '{SubscriptionId}' has been initiated", SubscriptionId);
        }

        /// <summary>
        /// Handles the event from the subscription.
        /// </summary>
        /// <param name="subscription">The subscription from which the event originated.</param>
        /// <param name="resolvedEvent">The event that was received.</param>
        /// <param name="ct">The cancellation token.</param>
        private async Task HandleEvent(StreamSubscription subscription, ResolvedEvent resolvedEvent, CancellationToken ct)
        {
            try
            {
                // If the event has no data or is a checkpoint event, we don't need to process it
                if (IsEventWithEmptyData(resolvedEvent) || IsCheckpointEvent(resolvedEvent)) return;

                var streamEvent = resolvedEvent.ToStreamEvent();

                if (streamEvent == null)
                {
                    // The streamEvent can be null if the event is from another module and cannot be deserialized.
                    // If we're not filtering out events from other modules, this can occur.
                    // In this case, it's safe to ignore the deserialization error.
                    // However, more sophisticated logic could be added to determine whether the error should be ignored.
                    _logger.LogWarning("Failed to deserialize the event with ID: {EventId}. This event may originate from another module.", resolvedEvent.Event.EventId);

                    if (!_subscriptionOptions.IgnoreDeserializationErrors)
                        throw new InvalidOperationException($"Failed to deserialize event {resolvedEvent.Event.EventType} with ID: {resolvedEvent.Event.EventId}. Deserialization errors are not being ignored.");
                    
                    return;
                }

                // If the stream event is not null, publish it to the internal event bus
                await _mediator.Publish(streamEvent, ct);

                // Publish the event to the projection publisher
                await _projectionPublisher.PublishAsync(streamEvent, ct);

                // Store the checkpoint for the event
                await _checkpointRepository.Store(SubscriptionId, resolvedEvent.Event.Position.CommitPosition, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the message: {ExceptionMessage}. Stack trace: {ExceptionStackTrace}",
                    ex.Message,
                    ex.StackTrace);

                // Consider whether it's acceptable to drop some events instead of stopping the subscription.
                // Currently, the events are being dropped, but error handling logic could be added.
                throw;
            }
        }

        /// <summary>
        /// Handles the event of a subscription drop.
        /// </summary>
        /// <param name="subscription">The subscription that was dropped.</param>
        /// <param name="reason">The reason why the subscription was dropped.</param>
        /// <param name="exception">The exception that caused the subscription to drop, if any.</param>
        private void HandleDrop(StreamSubscription _, SubscriptionDroppedReason reason, Exception exception)
        {
            _logger.LogError(
                exception,
                "Subscription to all '{SubscriptionId}' events dropped with the following reason: '{Reason}'",
                SubscriptionId,
                reason);

            if (exception is RpcException { StatusCode: StatusCode.Cancelled }) return;

            Resubscribe();
        }

        /// <summary>
        /// Attempts to resubscribe to the event stream after a subscription drop.
        /// </summary>
        private void Resubscribe()
        {
            // Consider implementing a maximum retry count to prevent infinite retries
            // when the database is not available or not ready to accept connections

            while (true)
            {
                var resubscribed = false;
                try
                {
                    // Lock to ensure only one thread attempts to resubscribe at a time
                    Monitor.Enter(resubscribeLock);

                    // Disable synchronization context to prevent deadlocks when running async methods.
                    // This is acceptable because this is a background process and doesn't need to maintain the async context.
                    using (NoSynchronizationContextScope.Enter())
                    {
                        SubscribeToAll(_subscriptionOptions, cancellationToken).Wait(cancellationToken);
                    }

                    resubscribed = true;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Failed to resubscribe to all events with ID '{SubscriptionId}'. The subscription was dropped due to the following error: {Message}. Stack trace: {StackTrace}",
                        SubscriptionId, exception.Message, exception.StackTrace);
                }
                finally
                {
                    Monitor.Exit(resubscribeLock);
                }

                if (resubscribed)
                    break;

                // Pause between reconnection attempts to avoid overloading the database or CPU
                // Add a random delay to reduce the likelihood of multiple subscriptions trying to reconnect simultaneously
                Thread.Sleep(1000 + new Random((int)DateTime.UtcNow.Ticks).Next(1000));
            }
        }

        /// <summary>
        /// Checks if the event has empty data.
        /// </summary>
        /// <param name="resolvedEvent">The event to check.</param>
        /// <returns>True if the event has empty data, false otherwise.</returns>
        private bool IsEventWithEmptyData(ResolvedEvent resolvedEvent)
        {
            if (resolvedEvent.Event.Data.Length != 0) return false;

            _logger.LogWarning("Received an event with no data. This event will not be processed.");
            return true;
        }

        /// <summary>
        /// Checks if the event is a checkpoint event.
        /// </summary>
        /// <param name="resolvedEvent">The event to check.</param>
        /// <returns>True if the event is a checkpoint event, false otherwise.</returns>
        private bool IsCheckpointEvent(ResolvedEvent resolvedEvent)
        {
            if (resolvedEvent.Event.EventType != EventTypeMapper.ToName<CheckpointStored>()) return false;

            _logger.LogInformation("Encountered a checkpoint event. This event will be ignored in the processing pipeline.");
            return true;
        }
    }
}
