using GhostLyzer.Core.Domain.Event;
using GhostLyzer.Core.Domain.Model;

namespace GhostLyzer.Core.EventStoreDB.Events
{
    /// <summary>
    /// Defines the interface for an aggregate in event sourcing.
    /// </summary>
    public interface IAggregateEventSourcing : IProjection, IEntity
    {
        /// <summary>
        /// Gets the list of domain events associated with the aggregate.
        /// </summary>
        IReadOnlyList<IDomainEvent> DomainEvents { get; }

        /// <summary>
        /// Clears the domain events associated with the aggregate.
        /// </summary>
        /// <returns>An array of the cleared events.</returns>
        IEvent[] ClearDomainEvents();

        /// <summary>
        /// Gets the version of the aggregate.
        /// </summary>
        long Version { get; }
    }

    /// <summary>
    /// Defines the interface for an aggregate in event sourcing with a specific type of identifier.
    /// </summary>
    /// <typeparam name="T">The type of the identifier.</typeparam>
    public interface IAggregateEventSourcing<out T> : IAggregateEventSourcing
    {
        /// <summary>
        /// Gets the identifier of the aggregate.
        /// </summary>
        T Id { get; }
    }
}
