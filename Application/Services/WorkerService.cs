using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooscape.Application.Config;
using Zooscape.Application.Events;
using Zooscape.Domain.Enums;
using Zooscape.Domain.ExtensionMethods;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Models;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Application.Services;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private readonly GameSettings _gameSettings;
    private readonly GameLogsConfiguration _gameLogsConfig;
    private readonly GlobalSeededRandomizer _rng;
    private readonly IZookeeperService _zookeeperService;
    private readonly IGameStateService _gameStateService;
    private readonly IEventDispatcher _signalREventDispatcher;
    private readonly IEventDispatcher _cloudEventDispatcher;
    private readonly IEventDispatcher _logStateEventDispatcher;
    private readonly IEventDispatcher _logDiffStateEventDispatcher;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private readonly List<(int tick, TimeSpan duration, double dutyCycle)> _ticksTiming;

    //noinspection csharpsquid:S107
    public WorkerService(
        ILogger<WorkerService> logger,
        IOptions<GameSettings> gameSettingsOptions,
        IOptions<GameLogsConfiguration> gameLogsConfigOptions,
        GlobalSeededRandomizer rng,
        IZookeeperService zookeeperService,
        IGameStateService gameStateService,
        [FromKeyedServices("signalr")] IEventDispatcher signalREventDispatcher,
        [FromKeyedServices("cloud")] IEventDispatcher cloudEventDispatcher,
        [FromKeyedServices("logState")] IEventDispatcher logStateEventDispatcher,
        [FromKeyedServices("logDiffState")] IEventDispatcher logDiffStateEventDispatcher,
        IHostApplicationLifetime applicationLifetime
    )
    {
        _logger = logger;
        _gameSettings = gameSettingsOptions.Value;
        _gameLogsConfig = gameLogsConfigOptions.Value;
        _rng = rng;
        _zookeeperService = zookeeperService;
        _gameStateService = gameStateService;
        _signalREventDispatcher = signalREventDispatcher;
        _cloudEventDispatcher = cloudEventDispatcher;
        _logStateEventDispatcher = logStateEventDispatcher;
        _logDiffStateEventDispatcher = logDiffStateEventDispatcher;
        _applicationLifetime = applicationLifetime;
        _ticksTiming = [];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting game engine with seed: {Seed}", _rng.Seed);
        _logger.LogInformation(
            "Map Size: {Width}x{Height}",
            _gameStateService.World.Width,
            _gameStateService.World.Height
        );

        try
        {
            // Add one zookeeper to game world for a start
            _gameStateService.AddZookeeper();

            if (!await WaitForGameReady(stoppingToken))
            {
                throw new TimeoutException("Timed out waiting for game ready");
            }
            await _cloudEventDispatcher.Dispatch(
                new CloudCallbackEvent(CloudCallbackEventType.Started)
            );
            await GameLoop(stoppingToken);
            await _cloudEventDispatcher.Dispatch(
                new CloudCallbackEvent(
                    CloudCallbackEventType.Finished,
                    _gameStateService.TickCounter
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game failed with exception");
            await _cloudEventDispatcher.Dispatch(
                new CloudCallbackEvent(
                    CloudCallbackEventType.Failed,
                    _gameStateService.TickCounter,
                    ex
                )
            );
        }
        finally
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("WorkerService stopped due to cancellation");
            }

            if (_gameLogsConfig.FullLogsEnabled)
                await _logStateEventDispatcher.Dispatch(new CloseAndFlushLogsEvent());

            if (_gameLogsConfig.DiffLogsEnabled)
                await _logDiffStateEventDispatcher.Dispatch(new CloseAndFlushLogsEvent());

            _applicationLifetime.StopApplication();
        }
    }

    private async Task<bool> WaitForGameReady(CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(_gameSettings.StartGameTimeout));

        while (!_gameStateService.IsReady)
        {
            try
            {
                await Task.Delay(100, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    _logger.LogError(ex, "Waiting cancelled by external request.");
                else
                    _logger.LogError(ex, "Timeout waiting for game ready state.");

                return false;
            }
        }

        return true;
    }

    public async Task GameLoop(CancellationToken stoppingToken)
    {
        using var gameClock = new PeriodicTimer(
            TimeSpan.FromMilliseconds(_gameSettings.TickDuration)
        );
        var stopWatch = Stopwatch.StartNew();

        while (await gameClock.WaitForNextTickAsync(stoppingToken))
        {
            stopWatch.Restart();

            _gameStateService.TickCounter++;

            if (_gameLogsConfig.FullLogsEnabled)
                await _logStateEventDispatcher.Dispatch(new GameStateEvent(_gameStateService));

            if (_gameLogsConfig.DiffLogsEnabled)
                await _logDiffStateEventDispatcher.Dispatch(new GameStateEvent(_gameStateService));

            // Step 1: Pop first command off each animal's queue
            var commands = new List<AnimalCommand>();
            var animalsWithoutCommands = new List<IAnimal>();
            foreach (var (_, animal) in _gameStateService.Animals)
            {
                var command = animal.GetNextCommand();

                if (command == null)
                    animalsWithoutCommands.Add(animal);
                else
                    commands.Add(command);
            }

            // Step 2: Sort all commands by time
            commands.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));

            // Step 3: Iterate over animal command
            foreach (var command in commands)
            {
                var animal = _gameStateService.Animals[command.BotId];

                // Step 4a: Use active power up.
                if (command.Action == BotAction.UseItem)
                {
                    _gameStateService.ActivatePowerUp(animal);
                }
                // Step 4b: Set direction
                else
                {
                    animal.SetDirection(command.Action.ToDirection());
                }

                // Step 5: Set new position
                _gameStateService.MoveAnimal(animal);

                // Step 6: Apply consequences of movement
                ApplyAnimalConsequences(animal);

                // Step 7: Update viability
                animal.IsViable = animal.Location != animal.SpawnPoint;

                // Step 8: Process power ups
                _gameStateService.ProcessPowerUps(animal);
            }

            // Step 3b: Iterate over animals for which no command was received
            foreach (var animal in animalsWithoutCommands)
            {
                // Step 6b: Apply consequences of movement
                ApplyAnimalConsequences(animal);

                // Step 7b: Update viability
                animal.IsViable = animal.Location != animal.SpawnPoint;

                // Step 8: Process power ups
                _gameStateService.ProcessPowerUps(animal);
            }

            // Step 9: Iterate over zookeepers
            foreach (var (id, zookeeper) in _gameStateService.Zookeepers)
            {
                // Step 10: Calculate new direction
                var newDirection = Helpers.TrackExecutionTime(
                    "CalculateZookeeperDirection",
                    () =>
                        _zookeeperService.CalculateZookeeperDirection(_gameStateService.World, id),
                    out var _
                );
                zookeeper.SetDirection(newDirection);

                // Step 11: Set new position
                _gameStateService.MoveZookeeper(zookeeper);

                // Step 12: Apply consequences
                ApplyZookeeperConsequences(zookeeper);
            }

            // Step 13: Process spawning


           _gameStateService.ProcessSpawning();

            

            foreach (var (id, animal) in _gameStateService.Animals)
            {
                await _cloudEventDispatcher.Dispatch(new UpdatePlayerEvent(id, animal.Score));
            }

            // Step 14: Check end conditions
            if (
                !_gameStateService.World.Cells.Cast<CellContents>().Contains(CellContents.Pellet)
                || _gameStateService.TickCounter >= _gameSettings.MaxTicks
            )
            {
                await GameOver();
                stopWatch.Stop();
                return;
            }

            // Step 15: Send game state to bots and visualisers
            await _signalREventDispatcher.Dispatch(new GameStateEvent(_gameStateService));

            var measuredTickDuration = stopWatch.Elapsed;
            var dutyCycle =
                1.0 * measuredTickDuration.TotalMilliseconds / _gameSettings.TickDuration;

            _ticksTiming.Add((_gameStateService.TickCounter, measuredTickDuration, dutyCycle));

            var tickMsg =
                $"Game tick {_gameStateService.TickCounter}, Duration = {measuredTickDuration.TotalMilliseconds:F2} / {_gameSettings.TickDuration}, Duty Cycle = {dutyCycle:F4}";

            // Track ticks that exceed duty cycle
            if (dutyCycle >= 1)
            {
                _logger.LogWarning(tickMsg);
            }
            else
            {
                _logger.LogInformation(tickMsg);
            }
        }
    }

    private async Task GameOver()
    {
        var numPelletsLeft = _gameStateService
            .World.Cells.Cast<CellContents>()
            .Count(contents => contents == CellContents.Pellet);
        _logger.LogInformation(
            "Game end conditions met. Game Over. There are {NumPelletsLeft} pellets left.",
            numPelletsLeft
        );

        if (_ticksTiming.Count > 0)
        {
            _logger.LogInformation(
                "There were {NumTicks} ticks that exceeded the duty cycle:",
                _ticksTiming.Where(x => x.dutyCycle >= 1).Count()
            );
            foreach (
                (int tick, TimeSpan duration, double dutyCycle) in _ticksTiming.Where(x =>
                    x.dutyCycle >= 1
                )
            )
            {
                _logger.LogInformation(
                    "Tick: {Tick}, Duration: {TotalMs:F2} / {TickDuration}, Duty Cycle: {DutyCycle:F4}",
                    tick,
                    duration.TotalMilliseconds,
                    _gameSettings.TickDuration,
                    dutyCycle
                );
            }

            var maxTickDuration = _ticksTiming.MaxBy(tuple => tuple.duration);
            _logger.LogInformation(
                "The longest running tick was {MaxDurationTick} with a duration of {TotalMs:F2}ms",
                maxTickDuration.tick,
                maxTickDuration.duration.TotalMilliseconds
            );

            _logger.LogInformation(
                "Average tick duration {AverageDuration:F2}ms for a duty cycle of {DutyCycle:F2}",
                _ticksTiming.Average(x => x.duration.TotalMilliseconds),
                _ticksTiming.Average(x => x.dutyCycle)
            );
        }

        foreach (var (captureGroup, value) in Helpers.TrackedExecutionTimes())
        {
            _logger.LogInformation(
                "Execution time for {CaptureGroup} (max/min/avg): {Max:F2}ms / {Min:F2}ms / {Avg:F2}ms",
                captureGroup,
                value.Max,
                value.Min,
                value.Avg
            );
        }

        var orderedAnimals = _gameStateService
            .Animals.Values.OrderBy(animal => animal.Score)
            .Reverse();
        int placement = 1;
        foreach (var animal in orderedAnimals)
        {
            _logger.LogInformation(
                "{Placement}: {Animal}, Score: {Score}, Captured: {CaputuredCount}, Power Ups Used: {PowerUpsUsed}",
                placement,
                animal.Nickname,
                animal.Score,
                animal.CapturedCounter,
                animal.PowerUpsUsed
            );
            await _cloudEventDispatcher.Dispatch(
                new UpdatePlayerEvent(animal.Id, animal.Score, animal.Score, placement++)
            );
        }
    }

    private void ApplyAnimalConsequences(IAnimal animal)
    {
        // Is the animal on its spawn point?
        if (animal.Location == animal.SpawnPoint)
        {
            animal.IncrementTimeOnSpawn();
            return;
        }

        var cellContents = _gameStateService.World.GetCellContents(animal.Location);

        // Did the animal collect a pellet?
        if (cellContents == CellContents.Pellet)
        {
            animal.AddToScore(
                _gameSettings.PointsPerPellet,
                _gameSettings.PowerUps.Types[PowerUpType.BigMooseJuice.ToName()].Value
            );
            _gameStateService.World.SetCellContents(animal.Location, CellContents.Empty);

            //adding pellet to list to be respawned
            int pelletRespawnTick = _gameStateService.GetTicksUntilPelletRespawn(
                _gameStateService.TickCounter
            );
            if (!_gameStateService.PelletsToRespawn.ContainsKey(pelletRespawnTick))
            {
                _gameStateService.PelletsToRespawn.Add(pelletRespawnTick, new List<GridCoords>());
            }
            _logger.LogInformation(
                "Respawning pellet at {AnimalLocation} in {PelletRespawnTick} ticks",
                animal.Location,
                pelletRespawnTick
            );
            _gameStateService.PelletsToRespawn[pelletRespawnTick].Add(animal.Location);
        }
        else
        {
            animal.ScoreStreak.CoolDown();
        }

        // Did the animal collect a power pellet?
        if (cellContents == CellContents.PowerPellet)
        {
            _logger.LogInformation("Animal {Animal} picked up a power pellet.", animal.Nickname);
            animal.AddToScore(
                _gameStateService.GetPowerPelletScore(),
                _gameSettings.PowerUps.Types[PowerUpType.BigMooseJuice.ToName()].Value
            );
            _gameStateService.World.SetCellContents(animal.Location, CellContents.Empty);
        }

        List<CellContents> powerUps =
        [
            CellContents.Scavenger,
            CellContents.ChameleonCloak,
            CellContents.BigMooseJuice,
        ];

        if (powerUps.Contains(cellContents) && animal.HeldPowerUp == null)
        {
            _logger.LogInformation(
                "Animal {Nickname} picked up a {CellContents}.",
                animal.Nickname,
                cellContents
            );
            animal.HeldPowerUp = cellContents.ToPowerUpType();
            _gameStateService.World.SetCellContents(animal.Location, CellContents.Empty);
        }

        // Did the animal run into a zookeeper?
        if (_gameStateService.Zookeepers.Any(z => z.Value.Location == animal.Location))
        {
            var newScore = animal.Score * (100 - _gameSettings.ScoreLossPercentage) / 100.0;
            animal.SetScore((int)newScore);

            animal.Capture();
        }
    }

    private void ApplyZookeeperConsequences(IZookeeper zookeeper)
    {
        // Did the zookeeper run into an animal?
        foreach (
            var animal in _gameStateService.Animals.Values.Where(a =>
                a.Location == zookeeper.Location
            )
        )
        {
            if (animal == null)
                return;

            var newScore = animal.Score * (100 - _gameSettings.ScoreLossPercentage) / 100.0;
            animal.SetScore((int)newScore);

            animal.Capture();

            animal.IncrementTimeOnSpawn();
        }
    }
}