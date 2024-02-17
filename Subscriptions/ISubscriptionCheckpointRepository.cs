namespace GhostLyzer.Core.EventStoreDB.Subscriptions
{
    /// <summary>
    /// Represents a repository for managing subscription checkpoints.
    /// </summary>
    public interface ISubscriptionCheckpointRepository
    {
        /// <summary>
        /// Loads the checkpoint for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The checkpoint, or null if not found.</returns>
        ValueTask<ulong?> Load(string subscriptionId, CancellationToken cancellationToken);

        /// <summary>
        /// Stores the checkpoint for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="position">The position of the checkpoint.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        ValueTask Store(string subscriptionId, ulong position, CancellationToken cancellationToken);
    }
}
