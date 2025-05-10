using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReferenceBot.Models;
using ReferenceBot.Services;
using System.Text.Json;

namespace ReferenceBot;

public class Program
{
    public static IConfigurationRoot Configuration;

    private static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", optional: false)
            .AddEnvironmentVariables();

        Configuration = builder.Build();
        var environmentIp = Environment.GetEnvironmentVariable("RUNNER_IPV4");
        var ip = !string.IsNullOrWhiteSpace(environmentIp)
            ? environmentIp
            : Configuration.GetSection("RunnerIP").Value;
        ip = ip.StartsWith("http://") ? ip : "http://" + ip;

        //var nickName =
        //    Environment.GetEnvironmentVariable("BOT_NICKNAME")
        //    ?? Configuration.GetSection("BotNickname").Value;

        var nickName = GenerateBotName();

        var token = Environment.GetEnvironmentVariable("Token") ?? Guid.NewGuid().ToString();

        var port = Configuration.GetSection("RunnerPort");

        var url = ip + ":" + port.Value + "/bothub";

        var connection = new HubConnectionBuilder()
            .WithUrl($"{url}", options =>
            {
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
            })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .WithAutomaticReconnect()
            .Build();

        var botService = new BotService();

        BotCommand botCommand = new BotCommand();

        connection.On<Guid>("Registered", (id) =>
        {
            Console.WriteLine($"Bot Registered with ID {id}");
            botService.SetBotId(id);

        });
        

        connection.On<GameState>(
            "GameState",
            (gamestate) =>
            {
                //Console.WriteLine($"GameState Received");
                try
                {

                    //Console.WriteLine($"GameState Animal {botService.GetBotId()}: {JsonConvert.SerializeObject(gamestate.Animals)}");
                    botCommand = botService.ProcessState(gamestate);
                    //Console.WriteLine($"ProcessState: {JsonConvert.SerializeObject(botCommand)}");
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"ProcessState: {JsonConvert.SerializeObject(ex)}"); ;
                }
            }
        );

        connection.On<String>(
            "Disconnect",
            async (reason) =>
            {
                //Console.WriteLine($"Server sent disconnect with reason: {reason}");
                await connection.StopAsync();
            }
        );

        connection.Closed += (error) =>
        {
            //Console.WriteLine($"Server closed with error: {error}");
            return Task.CompletedTask;
        };



        await Task.Delay(new Random().Next(500, 2000));
        await connection.StartAsync();
        //Console.WriteLine("Connected to bot hub");

        await connection.InvokeAsync("Register", token, nickName);
        //Console.WriteLine("Sent Register message");
        
        
    }

    public static string GenerateBotName()
    {
        string[] prefixes = { "Auto", "Cyber", "Mecha", "Neuro", "Tech", "Synth", "Robo", "Hyper" };
        string[] adjectives = { "Swift", "Silent", "Smart", "Loyal", "Stealthy", "Brave", "Clever", "Dynamic" };
        string[] nouns = { "Unit", "Drone", "AI", "Assistant", "Module", "Processor", "Bot", "System" };

        Random random = new Random();
        return $"{prefixes[random.Next(prefixes.Length)]}{adjectives[random.Next(adjectives.Length)]}{nouns[random.Next(nouns.Length)]}";
    }
}
