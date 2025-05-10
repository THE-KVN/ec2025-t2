using System.Collections;

namespace Zooscape.MapGenerator;

public class Stack
{
    private readonly ArrayList _stack;

    public IEnumerator GetEnumerator()
    {
        return _stack.GetEnumerator();
    }

    public int Count
    {
        get { return _stack.Count; }
    }

    public object Push(object o)
    {
        _stack.Add(o);
        return o;
    }

    public object? Pop()
    {
        if (_stack.Count > 0)
        {
            object? val = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            return val;
        }
        else
            return null;
    }

    public object? Top()
    {
        if (_stack.Count > 0)
            return _stack[^1];
        else
            return null;
    }

    public bool Empty()
    {
        return (_stack.Count == 0);
    }

    public Stack()
    {
        _stack = [];
    }
}
