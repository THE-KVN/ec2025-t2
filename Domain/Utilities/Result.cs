namespace Zooscape.Domain.Utilities;

public class Result<T>
{
    private readonly bool _isSuccess;
    private readonly T? _value;
    private readonly ResultError? _error;

    public bool IsSuccess => _isSuccess;
    public T? Value => _value;
    public ResultError? Error => _error;

    private Result(T value)
    {
        _isSuccess = true;
        _value = value;
        _error = default;
    }

    private Result(ResultError error)
    {
        _isSuccess = false;
        _value = default;
        _error = error;
    }

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(ResultError error) => new(error);
}
