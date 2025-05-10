using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Zooscape.Infrastructure.CloudIntegration.Enums;
using Zooscape.Infrastructure.CloudIntegration.Factories;
using Zooscape.Infrastructure.CloudIntegration.Models;

namespace Zooscape.Infrastructure.CloudIntegration.Services;

public class CloudIntegrationService : ICloudIntegrationService
{
    private readonly CloudSettings _appSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudIntegrationService> _logger;
    private readonly List<CloudPlayer> _players;

    public CloudIntegrationService(
        CloudSettings appSettings,
        ILogger<CloudIntegrationService> logger
    )
    {
        _appSettings = appSettings;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _appSettings.ApiKey);
        _logger = logger;
        _players = new List<CloudPlayer>();
    }

    public async Task Announce(
        CloudCallbackType callbackType,
        Exception? e = null,
        int? seed = null,
        int? ticks = null
    )
    {
        var cloudPayload = CloudCallbackFactory.Build(
            _appSettings.MatchId ?? "",
            callbackType,
            e,
            seed,
            ticks
        );
        if (cloudPayload.Players != null)
            cloudPayload.Players = _players;

        try
        {
            var result = await _httpClient.PostAsync(
                _appSettings.ApiUrl,
                cloudPayload,
                new JsonMediaTypeFormatter()
            );
            if (!result.IsSuccessStatusCode)
                _logger.LogWarning(
                    "Received non-success status code from cloud callback. Code: {statusCode}",
                    result.StatusCode
                );
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Failed to make cloud callback with error: {message}", ex.Message);
        }
    }

    public void AddPlayer(string playerId)
    {
        _players.Add(
            new CloudPlayer
            {
                FinalScore = 0,
                GamePlayerId = playerId,
                MatchPoints = 0,
                Placement = 0,
                PlayerParticipantId = playerId,
                Seed = "",
            }
        );
    }

    public void UpdatePlayer(
        string playerId,
        int? finalScore = null,
        int? matchPoints = null,
        int? placement = null
    )
    {
        _players.ForEach(player =>
        {
            if (player.GamePlayerId.Equals(playerId))
            {
                player.FinalScore = finalScore ?? player.FinalScore;
                player.MatchPoints = matchPoints ?? player.MatchPoints;
                player.Placement = placement ?? player.Placement;
            }
        });
    }
}
