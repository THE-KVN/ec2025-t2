using System.Collections.Generic;
using Zooscape.Domain.Utilities;

namespace Zooscape.Domain.Models;

public class AnimalQueue<T>
{
    public int _maxActions;
    public Queue<T> _actionQueue = new();
    public T? _lastCommand;

    public AnimalQueue(int maxActions)
    {
        _maxActions = maxActions;
    }

    public Result<int> Enqueue(T command)
    {
        if (_actionQueue.Count > _maxActions)
            return new ResultError("Command queue has reached capacity");

        _actionQueue.Enqueue(command);

        return _actionQueue.Count;
    }

    public T? Dequeue()
    {
        if (_actionQueue.Count == 0)
            return _lastCommand;

        var command = _actionQueue.Dequeue();

        if (command != null)
            _lastCommand = command;

        return command;
    }

    public T? Peek()
    {
        if (_actionQueue.Count == 0)
            return default;

        return _actionQueue.Peek();
    }

    public void Clear()
    {
        _actionQueue.Clear();
        _lastCommand = default;
    }

    public int Count => _actionQueue.Count;
}
