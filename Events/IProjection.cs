namespace GhostLyzer.Core.EventStoreDB.Events
{
    public interface IProjection
    {
        void When(object @event);
    }
}
