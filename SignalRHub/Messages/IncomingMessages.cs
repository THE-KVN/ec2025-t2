namespace Zooscape.Infrastructure.SignalRHub.Messages;

public class IncomingMessages
{
    public static readonly IncomingMessages Register = new("Register");
    public static readonly IncomingMessages RegisterVisualiser = new("RegisterVisualiser");
    public static readonly IncomingMessages BotCommand = new("BotCommand");

    private readonly string _value;

    private IncomingMessages(string value)
    {
        _value = value;
    }

    public static implicit operator string(IncomingMessages value)
    {
        return value._value;
    }
}
