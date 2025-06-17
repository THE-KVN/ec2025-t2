#region normal 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NETCoreBot.Models;
using NETCoreBot.Services;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Context;
using Serilog.Extensions.Logging;
using Zooscape.Application;
using Zooscape.Application.Config;
using Zooscape.Application.Events;
using Zooscape.Application.Services;
using Zooscape.Domain.Interfaces;
using Zooscape.Infrastructure.CloudIntegration.Enums;
using Zooscape.Infrastructure.CloudIntegration.Events;
using Zooscape.Infrastructure.CloudIntegration.Models;
using Zooscape.Infrastructure.CloudIntegration.Services;
using Zooscape.Infrastructure.SignalRHub.Config;
using Zooscape.Infrastructure.SignalRHub.Events;
using Zooscape.Infrastructure.SignalRHub.Hubs;
using Zooscape.Services;

using Zooscape.Domain.Utilities;
using Zooscape.Infrastructure.S3Logger;
using Zooscape.Infrastructure.S3Logger.Events;
using Zooscape.Infrastructure.S3Logger.Utilities;

using System.IO;
using Serilog.Sinks.File.GZip;
using Microsoft.Extensions.Configuration;
using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Information("Initialising cloud integration");

using (LogContext.PushProperty("ConsoleOnly", true))
{
    Log.Information("Initialising cloud integration");

    // Initialize CloudIntegrationService manually so we can announce failures early
    var cloudLog = new SerilogLoggerFactory(Log.Logger).CreateLogger<CloudIntegrationService>();
    CloudSettings cloudSettings = new();
    CloudIntegrationService cloudIntegrationService = new(cloudSettings, cloudLog);

    Log.Information("Announcing initialisation to cloud");
    await cloudIntegrationService.Announce(CloudCallbackType.Initializing);
    try
    {
        Log.Information("Initialising host");
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (context, _, serilogConfig) =>
                serilogConfig.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext()
            )
            .ConfigureServices(
                (context, services) =>
                {
                    var seed = context.Configuration.GetSection("GameSettings").GetValue<int>("Seed");
                    if (seed <= 0)
                        seed = new Random().Next();

                    services.AddSingleton(new GlobalSeededRandomizer(seed));

                    // Cloud Integration
                    services.AddSingleton<ICloudIntegrationService>(cloudIntegrationService);

                    // SignalR
                    services.Configure<SignalRConfigOptions>(
                        context.Configuration.GetSection("SignalR")
                    );
                    services.AddSignalR(options =>
                    {
                        options.EnableDetailedErrors = true;
                        options.MaximumReceiveMessageSize = 40000000;
                        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                    });

                    services.Configure<GameSettings>(
                        context.Configuration.GetSection("GameSettings")
                    );

                    services.Configure<GameLogsConfiguration>(
                        context.Configuration.GetSection("GameLogsConfiguration")
                    );

                    services.AddKeyedSingleton<IEventDispatcher, SignalREventDispatcher>("signalr");
                    services.AddKeyedSingleton<IEventDispatcher, CloudEventDispatcher>("cloud");
                    services.AddKeyedSingleton<IEventDispatcher, LogStateEventDispatcher>(
                        "logState"
                    );
                    services.AddKeyedSingleton<IEventDispatcher, LogDiffStateEventDispatcher>(
                    "logDiffState"
                    );

                    S3.LogDirectory =
                    Environment.GetEnvironmentVariable("LOG_DIR")
                    ?? Path.Combine(AppContext.BaseDirectory, "logs");

                    services.AddSingleton<IStreamingFileLogger>(
                    new StreamingFileLogger(
                        context
                            .Configuration.GetSection("GameLogsConfiguration")
                            .GetValue<bool>("FullLogsEnabled"),
                        S3.LogDirectory,
                        "gameLogs.log"
                    )
                    );

                    services.AddSingleton<IStreamingFileDiffLogger>(
                        new StreamingFileDiffLogger(
                            context
                                .Configuration.GetSection("GameLogsConfiguration")
                                .GetValue<bool>("DiffLogsEnabled"),
                            S3.LogDirectory,
                            "gameDiffLogs.log"
                        )
                    );

                    services.AddSingleton<IZookeeperService, ZookeeperService>();

                    services.AddTransient<BotHub>();

                    services.AddSingleton<IGameStateService, GameStateService>();

                    services.AddHostedService<WorkerService>();



                    services.AddSingleton<GameVisualizerService>(provider =>
                    {
                        var gamestateService = provider.GetRequiredService<IGameStateService>();
                        var hubContext = provider.GetRequiredService<IHubContext<BotHub>>();
                        return new GameVisualizerService(gamestateService, hubContext);
                    });

                    // Register GameStateRenderingService
                    services.AddHostedService<GameStateRenderingService>();


                }
            )
            .ConfigureWebHostDefaults(webBuilder =>
            {
                var port = 5433;
                Log.Information("Configuring SignalR to run on port {port}", port);
                webBuilder.UseUrls($"http://*:{port}");
                webBuilder.Configure(app =>
                {

                    app.UseRouting();
                    app.UseStaticFiles();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<BotHub>("/bothub");
                        endpoints.MapFallbackToFile("index.html"); // Serve index.html for all other routes
                    });



                });
            })
            .UseSerilog(
                (context, services, serilogConfig) =>
                    serilogConfig.ReadFrom.Configuration(context.Configuration)
            )
            .Build();



        Log.Information("Running host");
        //host.Run();


        host.RunAsync(); // Run Zooscape in the background

        // Wait for the server to be ready before starting bots
        await Task.Delay(5000);

        List<Task> botTasks = new();
        botTasks.Add(StartOmNomNomDebug());

        StartAnimalBot("Gorilla", "G1");
        //StartAnimalBot("Penguin", "P1");
        StartAnimalBot("Snake", "S1");
        //StartAnimalBot("Cobra", "C1");
        StartAnimalBot("Elephant", "E1");
        // Keep bots running
        await Task.WhenAll(botTasks);

        await host.WaitForShutdownAsync(); // Keep the app running

    }
    catch (Exception ex)
    {
        Log.Fatal($"Error starting host: {ex}");
        await cloudIntegrationService.Announce(CloudCallbackType.Failed, ex);
    }
    finally
    {
        Log.CloseAndFlush();
        await S3.UploadLogs();
        await cloudIntegrationService.Announce(CloudCallbackType.LoggingComplete);
        Console.WriteLine("Logs uploaded to S3");
    }

    //Function to Start 4 ReferenceBot Instances
    static void StartReferenceBots(int botCount)
    {
        string referenceBotPath = "C:\\Users\\Kevin Olckers\\Documents\\GitHub\\2025-Zooscape\\ReferenceBot\\bin\\Debug\\net8.0\\ReferenceBot.exe";

        if (!File.Exists(referenceBotPath))
        {
            Log.Error("ReferenceBot.exe not found at {path}", referenceBotPath);
            return;
        }

        for (int i = 0; i < botCount; i++)
        {
            var nickname = $"Bot_{i + 1}";
            var token = Guid.NewGuid().ToString();

            var startInfo = new ProcessStartInfo
            {
                FileName = referenceBotPath,
                Arguments = $"--nickname {nickname} --token {token}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (sender, e) => Log.Information($"[Bot {i + 1}] {e.Data}");
            process.ErrorDataReceived += (sender, e) => Log.Error($"[Bot {i + 1} ERROR] {e.Data}");

            Log.Information("Starting ReferenceBot {nickname}", nickname);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
    }

    static void StartAnimalBot(string animal, string nick)
    {
        // Build the path to the animal bot executable
        string referenceBotPath = $"C:\\Users\\Kevin Olckers\\Documents\\GitHub\\2025-Zooscape\\ZooAnimals\\{animal}\\NETCoreBot.exe";

        if (!File.Exists(referenceBotPath))
        {
            Log.Error("ReferenceBot.exe not found at {path}", referenceBotPath);
            return;
        }

        var nickname = nick;
        var token = Guid.NewGuid();

        var startInfo = new ProcessStartInfo
        {
            FileName = referenceBotPath,
            Arguments = $"--nickname {nickname} --token {token}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            // Set the working directory to the executable's folder.
            WorkingDirectory = Path.GetDirectoryName(referenceBotPath)
        };

        var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Log.Information($"[Bot {animal}] {e.Data}");
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Log.Error($"[Bot {animal} ERROR] {e.Data}");
        };

        // Start the process and begin asynchronous output reading.
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    static async Task StartOmNomNomDebug()
    {
        var url = "http://localhost:5433/bothub";

        var nickname = "OmNomNom";
        var token = Guid.NewGuid().ToString();

        var connection = new HubConnectionBuilder()
            .WithUrl($"{url}")
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .WithAutomaticReconnect()
            .Build();

        var botService = new BotService();
        BotCommand botCommand = new BotCommand();

        connection.On<Guid>("Registered", (id) => botService.SetBotId(id));
        connection.On<GameState>("GameState", (gamestate) =>
        {
            botCommand = botService.ProcessState(gamestate);
        });

        connection.On<string>("Disconnect", async (reason) =>
        {
            Console.WriteLine($"Server sent disconnect with reason: {reason}");
            await connection.StopAsync();
        });

        connection.Closed += async (error) =>
        {
            Console.WriteLine($"Server closed with error: {error}");
        };

        await connection.StartAsync();
        Console.WriteLine($"[{nickname}] Connected to bot hub");

        await connection.InvokeAsync("Register", token, nickname);
        Console.WriteLine($"[{nickname}] Sent Register message");

        while (connection.State == HubConnectionState.Connected || connection.State == HubConnectionState.Connecting)
        {
            if (botCommand == null || botCommand.Action < NETCoreBot.Enums.BotAction.Up || botCommand.Action > NETCoreBot.Enums.BotAction.Right)
            {
                await Task.Delay(15);
                continue;
            }

            if (botCommand != null)
            {
                //Console.WriteLine($"[{nickname}] Sent Command {botCommand.Action}");
                await connection.SendAsync("BotCommand", botCommand);
            }



            botCommand = null;
        }
    }
}



#endregion


//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Runtime.Intrinsics.X86;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.AspNetCore.SignalR.Client;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using NETCoreBot.Enums;
//using NETCoreBot.Models;
//using NETCoreBot.Services;
//using NETCoreBot.Strategy;
//using S3Logger;
//using S3Logger.Events;
//using S3Logger.Utilities;
//using Serilog;
//using Serilog.Context;
//using Serilog.Extensions.Logging;
//using Zooscape;
//using Zooscape.Application;
//using Zooscape.Application.Config;
//using Zooscape.Application.Events;
//using Zooscape.Application.Services;
//using Zooscape.Domain.Interfaces;
//using Zooscape.Infrastructure.CloudIntegration.Enums;
//using Zooscape.Infrastructure.CloudIntegration.Events;
//using Zooscape.Infrastructure.CloudIntegration.Models;
//using Zooscape.Infrastructure.CloudIntegration.Services;
//using Zooscape.Infrastructure.SignalRHub.Config;
//using Zooscape.Infrastructure.SignalRHub.Events;
//using Zooscape.Infrastructure.SignalRHub.Hubs;
//using Zooscape.Services;
//using static System.Formats.Asn1.AsnWriter;

//using (LogContext.PushProperty("ConsoleOnly", true))
//{
//    //int runsPerCombo = 20;
//    string roundName = "T1_DRAFT";

//    if (!File.Exists($"{roundName}_results.csv"))
//    {
//        //File.AppendAllText($"{roundName}_results.csv", "Timestamp,Average,ALPHA,VISITED_TILE_PENALTY_FACTOR,REVERSING_TO_PARENT_PENALTY,ZOOKEEPER_AVOIDANCE_FACTOR,ENEMY_PATH_AVOIDANCE,PELLET_BONUS,DANGER_THRESHOLD,MAX_PELLET_CANDIDATES,CORRIDOR_PENALTY,Scores\n");
//        File.AppendAllText($"{roundName}_results.csv", "Timestamp,Average,Scores\n");
//    }

//    // var sampleCombinations = ParameterSweeper.GenerateRandomSamplesFromDefinedValues();
//    int currentRound = 1;
//    //int totalRounds = sampleCombinations.Count * runsPerCombo;

//    int totalRounds = 100;

//    //foreach (var parameters in sampleCombinations)
//    //{
//    try
//    {
//        List<int> scores = new();

//        for (int i = 1; i <= totalRounds; i++)
//        {
//            Console.ForegroundColor = ConsoleColor.Yellow;
//            Console.WriteLine($"[Harness] Round {currentRound}/{totalRounds}");
//            Console.ForegroundColor = ConsoleColor.White;

//            int score;
//            try
//            {
//                var experimentTask = RunExperiment(null);
//                var timeout = Task.Delay(300000); // 5-minute safety timeout
//                var completed = await Task.WhenAny(experimentTask, timeout);
//                score = completed == timeout ? -1 : await experimentTask;
//            }
//            catch (Exception ex)
//            {
//                Log.Error(ex, "[Harness] Experiment execution failed");
//                score = -1;
//            }

//            if (score == -1)
//            {
//                Log.Warning("[Harness] Game run timed out before reaching natural end.");
//            }

//            scores.Add(score);
//            currentRound++;
//            await Task.Delay(1000);
//        }

//        int avg = scores.Where(s => s >= 0).Any() ? (int)scores.Where(s => s >= 0).Average() : 0;
//        //string csv = $"{DateTime.Now},{avg},{ParameterSweeper.FormatParametersCsv(parameters)},[{string.Join("/", scores)}]\n";
//        string csv = $"{DateTime.Now},{avg},[{string.Join("/", scores)}]\n";
//        File.AppendAllText($"{roundName}_results.csv", csv);
//        Log.Information($"[Harness] ✔ Avg: {avg} | Scores: {string.Join(", ", scores)}");
//    }
//    catch (Exception ex)
//    {
//        Console.ForegroundColor = ConsoleColor.Red;
//        Console.WriteLine($"[Harness] Round {currentRound}/{totalRounds} failed");
//        Console.ForegroundColor = ConsoleColor.White;
//        File.AppendAllText($"{roundName}_results.csv", $"{DateTime.Now} ERROR: {ex.Message}\n");
//    }
//    // }

//    Console.WriteLine("Done!");
//}

//async Task<int> RunExperiment(Dictionary<string, object>? parameters)
//{
//    if (parameters != null)
//    {
//        ParameterSweeper.ApplyParameters(parameters);
//    }
//    Cobra.PersistentPath = null;
//    Cobra.PersistentTarget = null;
//    Cobra.VisitedCounts.Clear();

//    KillZombieBots();

//    var host = BuildZooscapeHost();

//    try
//    {
//        Log.Information("[Harness] Starting host...");
//        await host.StartAsync();
//        await Task.Delay(2000);

//        var omNomNomTask = StartOmNomNomDebug();

//        StartAnimalBot("Cobra", "C1");
//        StartAnimalBot("Tiger", "T1");
//        StartAnimalBot("Snake", "S1");

//        Log.Information("[Harness] Bots started. Awaiting final score...");

//        var timeout = Task.Delay(300000); // 5-minute safety net for the game
//        var completed = await Task.WhenAny(omNomNomTask, timeout);
//        int score = completed == timeout ? -1 : await omNomNomTask;

//        await host.StopAsync();
//        return score;
//    }
//    catch (Exception ex)
//    {
//        Log.Error(ex, "[Harness] RunExperiment failed");
//        try { await host.StopAsync(); } catch { }
//        return -1;
//    }
//}

//void KillZombieBots()
//{
//    foreach (var proc in Process.GetProcessesByName("NETCoreBot"))
//    {
//        try { proc.Kill(); } catch { }
//    }
//}

//IHost BuildZooscapeHost()
//{
//    var cloudLog = new SerilogLoggerFactory(Log.Logger).CreateLogger<CloudIntegrationService>();
//    CloudSettings cloudSettings = new();
//    CloudIntegrationService cloudIntegrationService = new(cloudSettings, cloudLog);

//    var host = Host.CreateDefaultBuilder()
//        .ConfigureServices((context, services) =>
//        {
//            services.AddSingleton<ICloudIntegrationService>(cloudIntegrationService);
//            services.Configure<SignalRConfigOptions>(context.Configuration.GetSection("SignalR"));
//            services.AddSignalR(options =>
//            {
//                options.EnableDetailedErrors = true;
//                options.MaximumReceiveMessageSize = 40000000;
//                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
//                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
//            });
//            services.Configure<GameSettings>(context.Configuration.GetSection("GameSettings"));
//            services.Configure<S3Configuration>(context.Configuration.GetSection("S3Configuration"));
//            services.AddKeyedSingleton<IEventDispatcher, SignalREventDispatcher>("signalr");
//            services.AddKeyedSingleton<IEventDispatcher, CloudEventDispatcher>("cloud");
//            services.AddKeyedSingleton<IEventDispatcher, LogStateEventDispatcher>("logState");

//            S3.LogDirectory = Environment.GetEnvironmentVariable("LOG_DIR") ?? Path.Combine(AppContext.BaseDirectory, "logs");

//            services.AddSingleton<IStreamingFileLogger>(new StreamingFileLogger(S3.LogDirectory, "gameLogs.log"));
//            services.AddSingleton<IZookeeperService, ZookeeperService>();
//            services.AddTransient<BotHub>();
//            services.AddSingleton<IGameStateService, GameStateService>();
//            services.AddHostedService<WorkerService>();
//            services.AddSingleton<GameVisualizerService>(provider =>
//            {
//                var gamestateService = provider.GetRequiredService<IGameStateService>();
//                var hubContext = provider.GetRequiredService<IHubContext<BotHub>>();
//                return new GameVisualizerService(gamestateService, hubContext);
//            });
//            services.AddHostedService<GameStateRenderingService>();
//        })
//        .ConfigureWebHostDefaults(webBuilder =>
//        {
//            webBuilder.UseUrls("http://*:5433");
//            webBuilder.Configure(app =>
//            {
//                app.UseRouting();
//                app.UseStaticFiles();
//                app.UseEndpoints(endpoints =>
//                {
//                    endpoints.MapHub<BotHub>("/bothub");
//                    endpoints.MapFallbackToFile("index.html");
//                });
//            });
//        })
//        .UseSerilog((context, services, serilogConfig) =>
//            serilogConfig.ReadFrom.Configuration(context.Configuration))
//        .Build();

//    return host;
//}

//void StartAnimalBot(string animal, string nick)
//{
//    string referenceBotPath = $"C:\\Users\\Kevin Olckers\\Documents\\GitHub\\2025-Zooscape\\ZooAnimals\\{animal}\\NETCoreBot.exe";
//    if (!File.Exists(referenceBotPath)) return;

//    var token = Guid.NewGuid();
//    var startInfo = new ProcessStartInfo
//    {
//        FileName = referenceBotPath,
//        Arguments = $"--nickname {nick} --token {token}",
//        UseShellExecute = false,
//        RedirectStandardOutput = true,
//        RedirectStandardError = true,
//        CreateNoWindow = true,
//        WorkingDirectory = Path.GetDirectoryName(referenceBotPath)
//    };
//    var process = new Process { StartInfo = startInfo };
//    process.Start();
//    process.BeginOutputReadLine();
//    process.BeginErrorReadLine();
//}

//async Task<int> StartOmNomNomDebug()
//{
//    var url = "http://localhost:5433/bothub";
//    var nickname = "OmNomNom";
//    var token = Guid.NewGuid().ToString();

//    var connection = new HubConnectionBuilder()
//        .WithUrl($"{url}")
//        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug))
//        .WithAutomaticReconnect()
//        .Build();

//    var botService = new BotService();
//    BotCommand botCommand = new BotCommand();
//    int finalScore = 0;
//    var gameFinished = new TaskCompletionSource<int>();

//    connection.On<Guid>("Registered", id => botService.SetBotId(id));

//    connection.On<GameState>("GameState", async gamestate =>
//    {
//        botCommand = botService.ProcessState(gamestate);
//        var myBot = gamestate.Animals.FirstOrDefault(a => a.Id == botService.GetBotId());
//        if (myBot != null)
//        {
//            finalScore = myBot.Score;
//            if (gamestate.Cells.Count(c => c.Content == CellContent.Pellet) <= 5 || gamestate.Tick >= 1990)
//            {
//                await connection.StopAsync();
//                gameFinished.TrySetResult(finalScore);
//            }
//        }
//    });

//    connection.On<string>("Disconnect", async reason =>
//    {
//        await connection.StopAsync();
//        gameFinished.TrySetResult(finalScore);
//    });

//    connection.Closed += async error =>
//    {
//        gameFinished.TrySetResult(finalScore);
//        await Task.CompletedTask;
//    };

//    await connection.StartAsync();
//    await connection.InvokeAsync("Register", token, nickname);

//    _ = Task.Run(async () =>
//    {
//        while (connection.State == HubConnectionState.Connected || connection.State == HubConnectionState.Connecting)
//        {
//            if (botCommand == null || botCommand.Action < BotAction.Up || botCommand.Action > BotAction.Right)
//            {
//                await Task.Delay(20);
//                continue;
//            }
//            await connection.SendAsync("BotCommand", botCommand);
//            botCommand = null;
//        }
//    });

//    return await gameFinished.Task;
//}







