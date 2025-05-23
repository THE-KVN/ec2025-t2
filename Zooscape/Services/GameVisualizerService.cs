﻿using Microsoft.AspNetCore.SignalR;
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
                AverageExecutionTime = Elephant.AverageExecutionTime,
                CurrentTargetPellet = Elephant.CurrentTargetPellet != null ? new { Elephant.CurrentTargetPellet.X, Elephant.CurrentTargetPellet.Y } : null,
                PersistentPath = Elephant.PersistentPath?.Select(n => new { n.X, n.Y }),
                ContestedPelletsThisTick = Elephant.ContestedPelletsThisTick.Select(p => new { p.Item1, p.Item2 }),
                IsInDanger = Elephant.IsInDanger,
                GameStage = Elephant.GAME_STAGE.ToString(),
                LastMove = Elephant.LastMove.ToString(),
                a.Location,
                a.CurrentDirection,
                ExecutionTimeExceedCount = Elephant.ExecutionTimeExceedCount


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
            Width = Elephant.MapWidth,
            Height = Elephant.MapHeight
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

