using System;

namespace Zooscape.Application.Events;

public class CloudCallbackEvent
{
    public readonly CloudCallbackEventType Type;
    public readonly int? Ticks;
    public readonly Exception? Exception;

    public CloudCallbackEvent(
        CloudCallbackEventType type,
        int? ticks = null,
        Exception? exception = null
    )
    {
        Type = type;
        Ticks = ticks;
        Exception = exception;
    }
}
