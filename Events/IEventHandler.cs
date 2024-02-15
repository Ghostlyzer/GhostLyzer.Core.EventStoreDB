using GhostLyzer.Core.Domain.Event;
using MediatR;

namespace GhostLyzer.Core.EventStoreDB.Events
{
    public interface IEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IEvent { }
}
