//using Microsoft.AspNetCore.SignalR;
//using Zooscape.Domain.Models;
//using Zooscape.Domain.Enums;
//using Zooscape.Domain.Interfaces;
//using Zooscape
//using System.Linq;
//using System.Threading.Tasks;

//public class GameVisualizerService
//{
//    private readonly IWorld _world;
//    private readonly IHubContext<BotHub> _hubContext;

//    public GameVisualizerService(IWorld world, IHubContext<BotHub> hubContext)
//    {
//        _world = world;
//        _hubContext = hubContext;
//    }

//    public async Task RenderGameStateAsync()
//    {
//        var gameState = new
//        {
//            cells = _world.Cells,
//            animals = _world.Animals.Values.Select(a => new { a.Location.X, a.Location.Y }),
//            zookeepers = _world.Zookeepers.Values.Select(z => new { z.Location.X, z.Location.Y })
//        };

//        await _hubContext.Clients.All.SendAsync("ReceiveGameState", gameState);
//    }
//}
