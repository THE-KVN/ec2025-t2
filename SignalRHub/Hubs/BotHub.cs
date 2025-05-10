using Microsoft.AspNetCore.SignalR;
using Zooscape.Application.Events;
using Zooscape.Application.Services;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Models;
using Zooscape.Infrastructure.CloudIntegration.Services;
using Zooscape.Infrastructure.SignalRHub.Messages;
using Zooscape.Infrastructure.SignalRHub.Models;

namespace Zooscape.Infrastructure.SignalRHub.Hubs;

public class BotHub : Hub
{
    private readonly ICloudIntegrationService _cloudIntegrationService;
    private readonly ILogger<BotHub> _logger;
    private readonly IGameStateService _gameStateService;
    private readonly IHostEnvironment _env;

    public BotHub(
        ICloudIntegrationService cloudIntegrationService,
        ILogger<BotHub> logger,
        IGameStateService gameStateService,
        IHostEnvironment env
    )
    {
        _cloudIntegrationService = cloudIntegrationService;
        _logger = logger;
        _gameStateService = gameStateService;
        _env = env;
    }

    #region Hub Methods

    public override Task OnConnectedAsync()
    {
        // TODO: Implement any specific stuff we need.
        try
        {
            //_logger.LogInformation("Bot connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in OnConnected: {msg}", e.Message);
            throw;
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        //_logger.LogInformation("Bot disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private bool IsAlreadyRegistered(string connectionId)
    {
        if (_gameStateService.Visualisers.Contains(connectionId))
        {
            _logger.LogError(
                "Visualiser already registered on connection {connectionId}",
                connectionId
            );
            return true;
        }

        if (_gameStateService.BotIds.ContainsKey(connectionId))
        {
            _logger.LogError(
                "Bot ({Nickname}) already registered on connection {connectionId}",
                _gameStateService.BotIds[connectionId].Nickname,
                connectionId
            );
            return true;
        }

        return false;
    }

    #endregion

    #region Bot Methods

    [HubMethodName(nameof(IncomingMessages.Register))]
    public async Task Register(Guid token, string nickname)
    {
        var connectionId = Context.ConnectionId;

        if (IsAlreadyRegistered(connectionId))
        {
            return;
        }

        var bot = _gameStateService.AddAnimal(token, nickname);
        if (!bot.IsSuccess)
        {
            _logger.LogError(
                "Bot ({token}), with nickname {nickname} not registered. Error: {error}",
                token,
                nickname,
                bot.Error?.ToString()
            );
            return;
        }

        _cloudIntegrationService.AddPlayer(token.ToString());
        _gameStateService.BotIds.TryAdd(Context.ConnectionId, (token, nickname));

        await Clients.Caller.SendAsync(OutgoingMessages.Registered, token);
    }

    [HubMethodName(nameof(IncomingMessages.BotCommand))]
    public void BotCommand(BotCommand botCommand)
    {
        var connectionId = Context.ConnectionId;

        if (!_gameStateService.BotIds.TryGetValue(connectionId, out var bot))
        {
            _logger.LogError(
                "Command received from unregistered connectionId ({connectionId})",
                connectionId
            );
            return;
        }

        if (botCommand == null)
        {
            _logger.LogError(
     "Command received from unregistered bot ({botId})",
     bot.BotId
 );
            return; 
        }

        if (!Enum.IsDefined(typeof(BotAction), botCommand.Action))
        {
            _logger.LogError(
                "Invalid command ({action}) received from bot ({botNickname}).",
                botCommand.Action,
                bot.Nickname
            );
            return;
        }


        var enqueueResult = _gameStateService.EnqueueCommand(bot.BotId, botCommand);

        if (!enqueueResult.IsSuccess)
        {
            _logger.LogError(
                "Command ({action}) not enqueued for bot ({botId}). Error: {error}",
                botCommand.Action,
                bot.BotId,
                enqueueResult.Error?.ToString()
            );
            return;
        }

        if (bot.Nickname == "OmNomNom")
        {
            _logger.LogError(
            "Command ({action}) enqueued for bot ({botId}). Queue length: {queueLength}",
            botCommand.Action,
            bot.Nickname,
            enqueueResult.Value
        );
        }
        
    }

    #endregion

    #region Visualiser Methods

    [HubMethodName(nameof(IncomingMessages.RegisterVisualiser))]
    public async Task RegisterVisualiser()
    {
        if (_env.IsProduction())
            return;

        var connectionId = Context.ConnectionId;

        if (IsAlreadyRegistered(connectionId))
            return;

        _gameStateService.Visualisers.Add(connectionId);

        await Clients.Caller.SendAsync(OutgoingMessages.Registered, null);
    }

    //public async Task BroadcastGameState()
    //{
    //    var gameState = new
    //    {
    //        cells = ConvertCellsToSerializable(_gameStateService.World.Cells),
    //        animals = _gameStateService.Animals.Values.Select(a => new
    //        {
    //            a.Id,
    //            a.Nickname,
    //            a.Location.X,
    //            a.Location.Y,
    //            //a.SpawnPoint.X,
    //            //a.SpawnPoint.Y,
    //            a.Score,
    //            a.CapturedCounter,
    //            a.DistanceCovered,
    //            a.IsViable,
    //            a.TimeInCage,
    //            a._commandQueue,
    //        }),
    //        zookeepers = _gameStateService.Zookeepers.Values.Select(z => new
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
    //        Tick = _gameStateService.TickCounter,
    //    };

    //    await Clients.All.SendAsync("ReceiveGameState", gameState);
    //}

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

    #endregion
}
