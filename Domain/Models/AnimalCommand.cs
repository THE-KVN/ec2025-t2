using System;
using Zooscape.Domain.Enums;

namespace Zooscape.Domain.Models;

public class AnimalCommand : BotCommand, IComparable<AnimalCommand>
{
    public AnimalCommand(Guid AnimalId, BotAction action)
    {
        BotId = AnimalId;
        Action = action;
        TimeStamp = DateTime.UtcNow;
    }

    public Guid BotId { get; set; }

    public DateTime TimeStamp { get; set; }

    public int CompareTo(AnimalCommand? other)
    {
        return TimeStamp.CompareTo(other?.TimeStamp);
    }
}
