using System.Threading.Tasks;

namespace Zooscape.Application.Events;

public interface IEventDispatcher
{
    Task Dispatch<TEvent>(TEvent gameEvent)
        where TEvent : class;
}
