
using System.Collections.Concurrent;

namespace GhostLyzer.Core.EventStoreDB.Subscriptions
{
    /// <summary>
    /// Represents an in-memory repository for managing subscription checkpoints.
    /// </summary>
    public class InMemorySubscriptionCheckpointRepository : ISubscriptionCheckpointRepository
    {
        private readonly ConcurrentDictionary<string, ulong> checkpoints = new();

        /// <summary>
        /// Loads the checkpoint for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The checkpoint, or null if not found.</returns>
        public ValueTask<ulong?> Load(string subscriptionId, CancellationToken cancellationToken)
        {
            return new(checkpoints.TryGetValue(subscriptionId, out var checkpoint) ? checkpoint : null);
        }

        /// <summary>
        /// Stores the checkpoint for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="position">The position of the checkpoint.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public ValueTask Store(string subscriptionId, ulong position, CancellationToken cancellationToken)
        {
            checkpoints.AddOrUpdate(subscriptionId, position, (_, _) => position);

            return ValueTask.CompletedTask;
        }
    }
}
