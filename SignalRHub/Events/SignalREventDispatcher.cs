using Microsoft.AspNetCore.SignalR;
using Zooscape.Application.Events;
using Zooscape.Domain.Enums;
using Zooscape.Infrastructure.SignalRHub.Hubs;
using Zooscape.Infrastructure.SignalRHub.Messages;
using Zooscape.Infrastructure.SignalRHub.Models;

namespace Zooscape.Infrastructure.SignalRHub.Events;

public class SignalREventDispatcher(IHubContext<BotHub> hubContext) : IEventDispatcher
{
    public async Task Dispatch<TEvent>(TEvent gameEvent)
        where TEvent : class
    {

        if (gameEvent is GameStateEvent gameStateEvent)
        {
            var payload = new GameState(gameStateEvent.GameState);
            await hubContext.Clients.All.SendAsync(OutgoingMessages.GameState, payload);
        }

        //if (gameEvent is GameStateEvent gameStateEvent)
        //{
        //    var gameState = new
        //    {
        //        cells = ConvertCellsToSerializable(gameStateEvent.GameState.World.Cells),
        //        animals = gameStateEvent.GameState.Animals.Values.Select(a => new
        //        {
        //            a.Id,
        //            a.Nickname,
        //            a.Location.X,
        //            a.Location.Y,
        //            a.SpawnPoint.X
        //            a.SpawnPoint.Y,
        //            a.Score,
        //            a.CapturedCounter,
        //            a.DistanceCovered,
        //            a.IsViable,
        //            a.TimeInCage,
        //            a._commandQueue,
        //            a.CurrentMultiplier,
        //            a.TicksSinceLastScore,
        //        }),
        //        zookeepers = gameStateEvent.GameState.Zookeepers.Values.Select(z => new
        //        {
        //            z.Id,
        //            z.Nickname,
        //            z.Location.X,
        //            z.Location.Y,
        //            //z.SpawnPoint.X,
        //            //z.SpawnPoint.Y,
        //            z.CurrentDirection,
        //            z.TicksSinceTargetCalculated,
        //            CurrentTargetId = z.CurrentTarget,
        //            CurrentPath = z.CurrentPath?.Nodes.Select(n => new { n.Coords.X, n.Coords.Y })
        //        }),
        //        Tick = gameStateEvent.GameState.TickCounter,
        //    };

        //    await hubContext.Clients.All.SendAsync(OutgoingMessages.GameState, gameState);
        //}
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
