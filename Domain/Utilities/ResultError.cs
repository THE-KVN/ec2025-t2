namespace Zooscape.Domain.Utilities;

public class ResultError
{
    private readonly string _message;
    private readonly ResultError? _innerError;

    public string Message => _message;
    public ResultError? InnerError => _innerError;

    public ResultError(string message)
    {
        _message = message;
        _innerError = null;
    }

    public ResultError(string message, ResultError? innerError)
    {
        _message = message;
        _innerError = innerError;
    }

    public override string ToString()
    {
        if (_innerError == null)
            return _message;
        else
            return $"{_message} -> {_innerError}";
    }
}
