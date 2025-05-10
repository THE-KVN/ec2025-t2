using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NETCoreBot.Models;
using NETCoreBot.Services;

namespace NETCoreBot;

public class Program
{
    public static IConfigurationRoot Configuration;

    private static async Task Main(String[] args)
    {
        var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);

        Configuration = builder.Build();

        var environmentIp = Environment.GetEnvironmentVariable("RUNNER_IPV4");
        var ip = !string.IsNullOrEmpty(environmentIp)
            ? environmentIp
            : Configuration.GetSection("RunnerIP").Value;
        ip = ip.StartsWith("http://") ? ip : "http://" + ip;

        var nickName =
            Environment.GetEnvironmentVariable("BOT_NICKNAME")
            ?? Configuration.GetSection("BotNickname").Value;

        var token = Guid.NewGuid().ToString();

        var port = Configuration.GetSection("RunnerPort");

        var url = ip + ":" + port.Value + "/bothub";

        var connection = new HubConnectionBuilder()
            .WithUrl($"{url}")
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .WithAutomaticReconnect()
            .Build();

        var botService = new BotService();
        BotCommand? botCommand = new BotCommand();

        await Task.Delay(new Random().Next(500, 2000));
        await connection.StartAsync();
        //Console.WriteLine("Connected to Runner");

        connection.On<Guid>("Registered", (id) => botService.SetBotId(id));

        connection.On<String>(
            "Disconnect",
            async (reason) =>
            {
                //Console.WriteLine($"Server sent disconnect with reason: {reason}");
                await connection.StopAsync();
            }
        );

        connection.On<GameState>(
            "GameState",
            (gamestate) =>
            {
                botCommand = botService.ProcessState(gamestate);
            }
        );

        connection.Closed += (error) =>
        {
            //Console.WriteLine($"Server closed with error: {error}");
            return Task.CompletedTask;
        };

        await connection.InvokeAsync("Register", token, nickName);
        while (connection.State == HubConnectionState.Connected || connection.State == HubConnectionState.Connecting)
        {
            if (botCommand == null || botCommand.Action < Enums.BotAction.Up || botCommand.Action > Enums.BotAction.Right)
            {
                await Task.Delay(30);
                continue;
            }

            if (botCommand != null)
            {
                await connection.SendAsync("BotCommand", botCommand);
            }
            //Console.WriteLine($"Sent BotCommand message");
            botCommand = null;
        }
    }
}
