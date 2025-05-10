//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json;
//using Zooscape.Application.Config;
//using Zooscape.Application.Events;
//using Zooscape.Application.Services;
//using Zooscape.Domain.Enums;
//using Zooscape.Domain.ExtensionMethods;
//using Zooscape.Domain.Interfaces;
//using Zooscape.Domain.Models;

//namespace Zooscape.Application;

//public class WorkerService : BackgroundService
//{
//    private readonly ILogger<WorkerService> _logger;
//    private readonly GameSettings _gameSettings;
//    private readonly IZookeeperService _zookeeperService;
//    private readonly IGameStateService _gameStateService;
//    private readonly IEventDispatcher _signalREventDispatcher;
//    private readonly IEventDispatcher _cloudEventDispatcher;
//    private readonly IEventDispatcher _logStateEventDispatcher;
//    private readonly IHostApplicationLifetime _applicationLifetime;

//    public WorkerService(
//        ILogger<WorkerService> logger,
//        IOptions<GameSettings> options,
//        IZookeeperService zookeeperService,
//        IGameStateService gameStateService,
//        [FromKeyedServices("signalr")] IEventDispatcher signalREventDispatcher,
//        [FromKeyedServices("cloud")] IEventDispatcher cloudEventDispatcher,
//        [FromKeyedServices("logState")] IEventDispatcher logStateEventDispatcher,
//        IHostApplicationLifetime applicationLifetime
//    )
//    {
//        _logger = logger;
//        _gameSettings = options.Value;
//        _zookeeperService = zookeeperService;
//        _gameStateService = gameStateService;
//        _signalREventDispatcher = signalREventDispatcher;
//        _cloudEventDispatcher = cloudEventDispatcher;
//        _logStateEventDispatcher = logStateEventDispatcher;
//        _applicationLifetime = applicationLifetime;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            // Add one zookeeper to game world for a start
//            _gameStateService.AddZookeeper();


//            if (!await WaitForGameReady(stoppingToken))
//            {
//                throw new Exception("Timed out waiting for game ready");
//            }
//            await _cloudEventDispatcher.Dispatch(
//                new CloudCallbackEvent(CloudCallbackEventType.Started)
//            );
//            await GameLoop(stoppingToken);
//            await _cloudEventDispatcher.Dispatch(
//                new CloudCallbackEvent(
//                    CloudCallbackEventType.Finished,
//                    _gameStateService.TickCounter
//                )
//            );
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Game failed with exception");
//            await _cloudEventDispatcher.Dispatch(
//                new CloudCallbackEvent(
//                    CloudCallbackEventType.Failed,
//                    _gameStateService.TickCounter,
//                    ex
//                )
//            );
//        }
//        finally
//        {
//            if (stoppingToken.IsCancellationRequested)
//            {
//                _logger.LogDebug("WorkerService stopped due to cancellation");
//            }
//            else
//            {
//                _logger.LogError("WorkerService stopped unexpectedly");
//            }
//            await _logStateEventDispatcher.Dispatch(new CloseAndFlushLogsEvent());
//            _applicationLifetime.StopApplication();
//            await _cloudEventDispatcher.Dispatch(
//                new CloudCallbackEvent(CloudCallbackEventType.LoggingComplete)
//            );
//            await StopAsync(stoppingToken);
//        }
//    }

//    private async Task<bool> WaitForGameReady(CancellationToken cancellationToken)
//    {
//        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
//            cancellationToken
//        );
//        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(_gameSettings.StartGameTimeout));

//        while (!_gameStateService.IsReady)
//        {
//            try
//            {
//                await Task.Delay(100, cancellationTokenSource.Token);
//            }
//            catch (TaskCanceledException)
//            {
//                if (cancellationToken.IsCancellationRequested)
//                    _logger.LogError("Waiting cancelled by external request.");
//                else
//                    _logger.LogError("Timeout waiting for game ready state.");

//                return false;
//            }
//        }

//        return true;
//    }

//    public async Task GameLoop(CancellationToken stoppingToken)
//    {
//        using var gameClock = new PeriodicTimer(
//            TimeSpan.FromMilliseconds(_gameSettings.TickDuration)
//        );
//        var stopWatch = Stopwatch.StartNew();

//        while (await gameClock.WaitForNextTickAsync(stoppingToken))
//        {
//            stopWatch.Restart();

//            _gameStateService.TickCounter++;

//            // Step 0b: Add additional zookeepers dynamically
//            //if (_gameStateService.TickCounter == 100 ||
//            //    (_gameStateService.TickCounter > 100 && (_gameStateService.TickCounter - 100) % 300 == 0))
//            //{
//            //    _logger.LogInformation($"Tick {_gameStateService.TickCounter}: Adding a new zookeeper");
//            //    _gameStateService.AddZookeeper();
//            //}


//            await _logStateEventDispatcher.Dispatch(new GameStateEvent(_gameStateService));

//            // Step 1: Pop first command off each animal's queue
//            var commands = new List<AnimalCommand>();
//            var animalsWithoutCommands = new List<IAnimal>();
//            foreach (var (_, animal) in _gameStateService.Animals)
//            {
//                var command = animal.GetNextCommand();

//                if (command == null)
//                    animalsWithoutCommands.Add(animal);
//                else
//                    commands.Add(command);
//            }

//            // Step 2: Sort all commands by time
//            commands.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));

//            // Step 3: Iterate over animal command
//            foreach (var command in commands)
//            {
//                var animal = _gameStateService.Animals[command.BotId];

//                // Log the animal ID
//                //Console.WriteLine($"Processing command for animal with ID: {animal.Id}");

//                // Step 4: Set direction
//                animal.SetDirection(command.Action.ToDirection());

//                // Step 5: Set new position
//                _gameStateService.MoveAnimal(animal);

//                // Step 6: Apply consequences of movement
//                ApplyAnimalConsequences(animal);

//                // Step 7: Update viability
//                animal.IsViable = animal.Location != animal.SpawnPoint;
//            }

//            // Step 3b: Iterate over animals for which no command was received
//            foreach (var animal in animalsWithoutCommands)
//            {
//                // Step 6b: Apply consequences of movement
//                ApplyAnimalConsequences(animal);

//                // Step 7b: Update viability
//                animal.IsViable = animal.Location != animal.SpawnPoint;
//            }

//            // Step 8: Iterate over zookeepers
//            foreach (var (id, zookeeper) in _gameStateService.Zookeepers)
//            {
//                // Step 9: Calculate new direction
//                var newDirection = _zookeeperService.CalculateZookeeperDirection(
//                    _gameStateService.World,
//                    id
//                );
//                zookeeper.SetDirection(newDirection);

//                // Step 10: Set new position
//                _gameStateService.MoveZookeeper(zookeeper);

//                // Step 11: Apply consequences
//                ApplyZookeeperConsequences(zookeeper);
//            }

//            foreach (var (id, animal) in _gameStateService.Animals)
//            {
//                await _cloudEventDispatcher.Dispatch(new UpdatePlayerEvent(id, animal.Score));
//            }

//            // Step 12: Check end conditions
//            if (
//                !_gameStateService.World.Cells.Cast<CellContents>().Contains(CellContents.Pellet)
//                || _gameStateService.TickCounter >= _gameSettings.MaxTicks
//            )
//            {
//                var numPelletsLeft = _gameStateService
//                    .World.Cells.Cast<CellContents>()
//                    .Count(contents => contents == CellContents.Pellet);
//                _logger.LogInformation(
//                    $"Game end conditions met. Game Over. There are {numPelletsLeft} pellets left."
//                );
//                var orderedAnimals = _gameStateService
//                    .Animals.Values.OrderBy(animal => animal.Score)
//                    .Reverse();
//                int placement = 1;
//                foreach (var animal in orderedAnimals)
//                {
//                    _logger.LogInformation(
//                        $"{placement}: {animal.Nickname}, Score: {animal.Score}, Captured: {animal.CapturedCounter}"
//                    );
//                    await _cloudEventDispatcher.Dispatch(
//                        new UpdatePlayerEvent(animal.Id, animal.Score, animal.Score, placement++)
//                    );
//                }
//                stopWatch.Stop();
//                return;
//            }

//            // Step 13: Send game state to bots and visualisers
//            // Log the animal ID
//            //Console.WriteLine($"Dispatching: {JsonConvert.SerializeObject(_gameStateService.Animals)}");
//            await _signalREventDispatcher.Dispatch(new GameStateEvent(_gameStateService));

//            var measuredTickDuration = stopWatch.Elapsed.TotalMilliseconds;
//            var dutyCycle = 1.0 * measuredTickDuration / _gameSettings.TickDuration;


//            _logger.LogInformation(
//                            $"Game tick {_gameStateService.TickCounter}, Duration = {Math.Round(measuredTickDuration, 2)} / {_gameSettings.TickDuration}, Duty Cycle = {Math.Round(dutyCycle, 4)}"
//                        );
//        }
//    }

//    private void ApplyAnimalConsequences(IAnimal animal)
//    {
//        // Is the animal on its spawn point?
//        if (animal.Location == animal.SpawnPoint)
//        {
//            animal.IncrementTimeOnSpawn();
//            return;
//        }

//        // Did the animal collect a pellet?
//        if (_gameStateService.World.GetCellContents(animal.Location) == CellContents.Pellet)
//        {
//            animal.SetScore(animal.Score + _gameSettings.PointsPerPellet);
//            _gameStateService.World.SetCellContents(animal.Location, CellContents.Empty);
//        }

//        // Did the animal run into a zookeeper?
//        if (_gameStateService.Zookeepers.Any(z => z.Value.Location == animal.Location))
//        {
//            var newScore = animal.Score * (100 - _gameSettings.ScoreLossPercentage) / 100.0;
//            animal.SetScore((int)newScore);

//            animal.Capture();
//        }
//    }

//    private void ApplyZookeeperConsequences(IZookeeper zookeeper)
//    {
//        // Did the zookeeper run into an animal?
//        foreach (
//            var animal in _gameStateService.Animals.Values.Where(a =>
//                a.Location == zookeeper.Location
//            )
//        )
//        {
//            if (animal == null)
//                return;

//            var newScore = animal.Score * (100 - _gameSettings.ScoreLossPercentage) / 100.0;
//            animal.SetScore((int)newScore);

//            animal.Capture();

//            animal.IncrementTimeOnSpawn();
//        }
//    }
//}
