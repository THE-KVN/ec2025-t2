using System;

namespace Zooscape.Domain.Enums;

public static class PowerUpNames
{
    public const string PowerPellet = "PowerPellet";
    public const string ChameleonCloak = "ChameleonCloak";
    public const string Scavenger = "Scavenger";
    public const string BigMooseJuice = "BigMooseJuice";
}

public enum PowerUpType
{
    PowerPellet = 0,
    ChameleonCloak = 1,
    Scavenger = 2,
    BigMooseJuice = 3,
}

public static class PowerUpTypeExtensions
{
    public static string ToName(this PowerUpType powerUpType)
    {
        return powerUpType switch
        {
            PowerUpType.PowerPellet => PowerUpNames.PowerPellet,
            PowerUpType.ChameleonCloak => PowerUpNames.ChameleonCloak,
            PowerUpType.Scavenger => PowerUpNames.Scavenger,
            PowerUpType.BigMooseJuice => PowerUpNames.BigMooseJuice,
            _ => throw new ArgumentException($"Invalid enum value: {powerUpType}"),
        };
    }

    public static PowerUpType FromName(string name)
    {
        return name switch
        {
            PowerUpNames.PowerPellet => PowerUpType.PowerPellet,
            PowerUpNames.ChameleonCloak => PowerUpType.ChameleonCloak,
            PowerUpNames.Scavenger => PowerUpType.Scavenger,
            PowerUpNames.BigMooseJuice => PowerUpType.BigMooseJuice,
            _ => throw new ArgumentException($"Invalid power up name: {name}"),
        };
    }
}