using System;

namespace Zooscape.Domain.Models;

public class ScoreStreak
{
    public double Multiplier { get; set; }
    public int MissedPellets { get; set; }
    public double GrowthFactor { get; init; }
    public double Max { get; init; }
    public int GracePeriod { get; init; }

    public ScoreStreak(double growthFactor, double max, int gracePeriod)
    {
        Multiplier = 1;
        MissedPellets = 0;
        GrowthFactor = growthFactor;
        Max = max;
        GracePeriod = gracePeriod;
    }

    public void Grow()
    {
        Multiplier = Math.Min(Multiplier + GrowthFactor, Max);
        MissedPellets = 0;
    }

    public void CoolDown()
    {
        if (++MissedPellets > GracePeriod)
            Reset();
    }

    public void Reset()
    {
        Multiplier = 1;
        MissedPellets = 0;
    }
}