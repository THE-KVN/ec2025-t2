using Microsoft.AspNetCore.SignalR;
using Zooscape.Domain.Models;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using Zooscape.Infrastructure.SignalRHub.Hubs;
using System.Collections.Generic;
using Zooscape.Infrastructure.SignalRHub.Models;
using Zooscape.Application.Events;
using Zooscape.Application.Services;
using NETCoreBot.Strategy;
using Newtonsoft.Json;
using System;

public class GameVisualizerService
{
    private readonly IGameStateService _gameStateService;
    private readonly IHubContext<BotHub> _hubContext;

    public GameVisualizerService(IGameStateService gameStateService, IHubContext<BotHub> hubContext)
    {
        _gameStateService = gameStateService;
        _hubContext = hubContext;
    }

    public async Task RenderGameStateAsync()
    {

     

        var gameState = new
        {
            cells = ConvertCellsToSerializable(_gameStateService.World.Cells),
            animals = _gameStateService.Animals.Values.Select(static a => new
            {
                
                a.Id,
                a.Nickname,
                a.Location.X,
                a.Location.Y,
                a.Score,
                a.CapturedCounter,
                a.DistanceCovered,
                a.IsViable,
                a.TimeInCage,
                AverageExecutionTime = "0",
                CurrentTargetPellet = Gorilla.PersistentTarget != null ? new { Gorilla.PersistentTarget.X, Gorilla.PersistentTarget.Y } : null,
                PersistentPath = Gorilla.PersistentPath?.Select(n => new { n.X, n.Y }),
                ContestedPelletsThisTick = Gorilla.CONTESTED_CELLS_MAP.Select(p => new { p.Item1, p.Item2 }),
                CorridorCells = Gorilla.CORRIDOR_CELLS_MAP.Select(c => new { c.Item1, c.Item2 }),
                IsInDanger = Gorilla.IsInDanger,
                GameStage = Gorilla.GAME_STAGE.ToString(),
                LastMove = Gorilla.LastMove.ToString(),
                a.Location,
                a.CurrentDirection,
                ExecutionTimeExceedCount = Gorilla.ExecutionTimeExceedCount,
                CurrentMultiplier = a.ScoreStreak.Multiplier,
                BestCluster = Gorilla.BestCluster,
                SafetyNetMap = Gorilla.SAFETY_NET_MAP?.Select(c => new { c.Item1, c.Item2 }),
                HeldPowerUp = a.HeldPowerUp.HasValue ? a.HeldPowerUp.Value.ToString() : "none",
                ActivePowerUp = a.ActivePowerUp != null ? a.ActivePowerUp.Value.ToString() : "none",
            }),
            zookeepers = _gameStateService.Zookeepers.Values.Select(z => new
            {
                z.Id,
                z.Nickname,
                z.Location.X,
                z.Location.Y,
                z.CurrentDirection,
                z.TicksSinceTargetCalculated,
                CurrentTargetId = z.CurrentTarget,
                CurrentPath = z.CurrentPath?.Nodes.Select(n => new { n.Coords.X, n.Coords.Y })
            }),
            Tick = _gameStateService.TickCounter,
            Width = Gorilla.MapWidth,
            Height = Gorilla.MapHeight
        };

       
        await _hubContext.Clients.All.SendAsync("ReceiveGameState", gameState);
    }

    private List<Cell> ConvertCellsToSerializable(CellContents[,] cells)
    {
        var cellList = new List<Cell>();

        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                cellList.Add(new Cell
                {
                    X = x,
                    Y = y,
                    Content = (CellContents)cells[x, y]
                });
            }
        }

        return cellList;
    }
}

