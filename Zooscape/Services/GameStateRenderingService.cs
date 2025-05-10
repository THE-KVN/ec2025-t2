using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Zooscape.Services
{
    public class GameStateRenderingService : IHostedService, IDisposable
    {
        private readonly GameVisualizerService _gameVisualizerService;
        private Timer _timer;

        public GameStateRenderingService(GameVisualizerService gameVisualizerService)
        {
            _gameVisualizerService = gameVisualizerService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(RenderGameState, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(200)); // Adjust interval as needed
            return Task.CompletedTask;
        }

        private async void RenderGameState(object state)
        {
            await _gameVisualizerService.RenderGameStateAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
