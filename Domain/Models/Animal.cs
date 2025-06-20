using System;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Models;

public class Animal : IAnimal
{
    public readonly AnimalQueue<AnimalCommand> _commandQueue;

    public Guid Id { get; init; }
    public string Nickname { get; init; }
    public GridCoords Location { get; private set; }
    public GridCoords SpawnPoint { get; init; }
    public Direction CurrentDirection { get; private set; }
    public int Score { get; private set; }
    public int TicksOnSpawn { get; private set; }
    public DateTime? FirstCommandTimeStamp { get; private set; }
    public int CapturedCounter { get; private set; }
    public int DistanceCovered { get; private set; }
    public bool IsViable { get; set; } = false;
    public AnimalCommand FirstCommand { get; set; }
    public int TimeInCage { get; private set; }
    public ScoreStreak ScoreStreak { get; }
    public ActivePowerUp? ActivePowerUp { get; set; }
    public PowerUpType? HeldPowerUp { get; set; }
    public int PowerUpsUsed { get; set; }



    AnimalQueue<AnimalCommand> IAnimal._commandQueue => _commandQueue;

    public Animal(
        Guid id,
        string nickname,
        GridCoords spawnPoint,
        int queueSize,
        double scoreStreakGrowthFactor,
        double scoreStreakMax,
        int scoreStreakGracePeriod
    )
    {
        Id = id;
        Nickname = nickname;
        SpawnPoint = spawnPoint;
        Location = spawnPoint;
        CurrentDirection = Direction.Idle;
        Score = 0;
        CapturedCounter = 0;
        DistanceCovered = 0;
        ScoreStreak = new ScoreStreak(
        scoreStreakGrowthFactor,
        scoreStreakMax,
        scoreStreakGracePeriod
        );

        _commandQueue = new AnimalQueue<AnimalCommand>(queueSize);
    }

    public Result<int> AddCommand(AnimalCommand command)
    {
        FirstCommandTimeStamp ??= command.TimeStamp;

        return _commandQueue.Enqueue(command);
    }

    public AnimalCommand? GetNextCommand()
    {
        return _commandQueue.Dequeue();
    }

    public void SetDirection(Direction newDirection)
    {
        CurrentDirection = newDirection;
    }

    public void SetLocation(GridCoords newLocation)
    {
        if (newLocation != Location)
            DistanceCovered++;

        Location = newLocation;
    }

    public void SetScore(int newScore)
    {
        Score = newScore;
    }

    public void AddToScore(int points, double multiplier)
    {
        ////EDIT
        if (ActivePowerUp != null && ActivePowerUp.Type == PowerUpType.BigMooseJuice)
        {
            Score += (int)(points * multiplier);
        }
        Score = Score + (int)(points * ScoreStreak.Multiplier);
        ScoreStreak.Grow();


        //Score += points;

    }

    public void IncrementTimeOnSpawn()
    {
        TicksOnSpawn++;
    }

    public void Capture()
    {
        CapturedCounter++;
        Location = SpawnPoint;
        _commandQueue.Clear();
        ScoreStreak.Reset();
    }
}
