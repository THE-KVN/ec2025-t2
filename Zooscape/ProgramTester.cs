//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.ML;
//using Microsoft.ML.Data;
//using Microsoft.ML.Trainers.LightGbm;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.AspNetCore.SignalR.Client;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Serilog;
//using Serilog.Context;
//using Serilog.Extensions.Logging;

//using NETCoreBot.Strategy;
//using NETCoreBot.Models;
//using NETCoreBot.Services;
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
//using NETCoreBot.Enums;
//using System.Runtime.Intrinsics.X86;
//using static System.Formats.Asn1.AsnWriter;

//namespace Zooscape
//{
//    public class ProgramTester
//    {
//        const int numberOfParameters = 9;
//        const int seedFactor = 10;              // 5–10
//        const int iterFactor = 100;              // 20–50 (up to 100 if you dare)
//        const int InitialSamples = numberOfParameters * seedFactor;  // 9*10 = 90
//        const int Iterations = numberOfParameters * iterFactor;  // 9*50 = 450
//        const int CandidatePool = 100;             // 20–200
//        const int RunsPerCombo = 5;              // 1–5 (or more if you want to be sure)


//        static readonly MLContext mlContext = new MLContext(seed: seedFactor);

//        public static async Task Main(string[] args)
//        {
//            var data = new List<ParamEval>();

//            await RunSigmaTestAsync();


//            string roundName = "KC_EG_01";

//            if (!File.Exists($"{roundName}_results.csv"))
//            {
//                    File.AppendAllText($"{roundName}_results.csv", "Timestamp,Average,ALPHA,VISITED_TILE_PENALTY_FACTOR,REVERSING_TO_PARENT_PENALTY,ZOOKEEPER_AVOIDANCE_FACTOR,ENEMY_PATH_AVOIDANCE,PELLET_BONUS,DANGER_THRESHOLD,MAX_PELLET_CANDIDATES,CORRIDOR_PENALTY,Scores\n");
//            }


//            // 1) Seed with random exploration
//            for (int i = 0; i < InitialSamples; i++)
//            {
//                var p = ParameterSweeper.GenerateRandomParameter();
//                var scores = Enumerable.Range(0, RunsPerCombo).Select(_ => RunExperiment(p).GetAwaiter().GetResult()) .ToList();
//                double avg = scores.Average();

//                string csv = $"{DateTime.Now},{avg},{ParameterSweeper.FormatParametersCsv(p)},[{string.Join("/", scores)}]\n";
//                File.AppendAllText($"{roundName}_results.csv", csv);

//                data.Add(new ParamEval(p, avg));
//                Console.WriteLine($"[Init] {i + 1}/{InitialSamples} → score={avg}");
//            }




//            // 2) ML-guided loop
//            for (int iter = 0; iter < Iterations; iter++)
//            {
//                // 2a) Train surrogate model on all seen ⟨params,score⟩
//                var trainView = mlContext.Data.LoadFromEnumerable(data);
//                var pipeline = mlContext.Transforms
//                    .Concatenate("Features",
//                        nameof(ParamEval.Alpha),
//                        nameof(ParamEval.VisitedTilePenalty),
//                        nameof(ParamEval.ReversingPenalty),
//                        nameof(ParamEval.ZookeeperAvoidance),
//                        nameof(ParamEval.EnemyPathAvoidance),
//                        nameof(ParamEval.PelletBonus),
//                        nameof(ParamEval.DangerThreshold),
//                        nameof(ParamEval.MaxPelletCandidates),
//                        nameof(ParamEval.CorridorPenalty))
//                    .Append(mlContext.Regression.Trainers.LightGbm(
//                        new LightGbmRegressionTrainer.Options
//                        {
//                            NumberOfLeaves = 31,
//                            MinimumExampleCountPerLeaf = 5
//                        }));
//                var model = pipeline.Fit(trainView);
//                var predictor = mlContext.Model.CreatePredictionEngine<ParamEval, Pred>(model);

//                // 2b) Sample many random candidates, pick the one with highest predicted score
//                var seenKeys = new HashSet<string>(data.Select(d => d.UniqueKey));
//                ParamEval bestCand = null;
//                double bestPred = double.MinValue;

//                for (int c = 0; c < CandidatePool; c++)
//                {
//                    var dict = ParameterSweeper.GenerateRandomParameter();
//                    var pe = new ParamEval(dict, 0);

//                    if (!seenKeys.Add(pe.UniqueKey))
//                        continue;

//                    double pred = predictor.Predict(pe).Score;
//                    if (pred > bestPred)
//                    {
//                        bestPred = pred;
//                        bestCand = pe;
//                    }
//                }

//                // 2c) Evaluate that best-predicted candidate in the real game
//                var bestParams = bestCand.ToDictionary();
//                // evaluate the same params RunsPerCombo times
//                var scores = new List<int>();
//                for (int i = 0; i < RunsPerCombo; i++)
//                    scores.Add(await RunExperiment(bestParams));

//                double avgScore = scores.Average();

//                string csvs = $"{DateTime.Now},{avgScore},{ParameterSweeper.FormatParametersCsv(bestParams)},[{string.Join("/", scores)}]\n";
//                File.AppendAllText($"{roundName}_results.csv", csvs);

//                data.Add(new ParamEval(bestParams, avgScore));
//                Console.WriteLine(
//                    $"Evaluated {RunsPerCombo}×: scores=[{string.Join(",", scores)}], avg={avgScore}");


//                Console.WriteLine($"[Iter {iter + 1}/{Iterations}] predicted={bestPred:F1}, actual={avgScore}");
//            }

//            // 3) Done—report best seen
//            var bestOverall = data.OrderByDescending(d => d.Score).First();

//            string csvd = $"{DateTime.Now},{bestOverall.Score},{bestOverall.UniqueKey}]\n";
//            File.AppendAllText($"{roundName}_results.csv", csvd);

//            Console.WriteLine("=== BEST FOUND ===");
//            Console.WriteLine($"Score = {bestOverall.Score}");
//            Console.WriteLine($"Params= {bestOverall.UniqueKey}");
//        }

//        /// <summary>
//        /// Maps a single parameter‐set to your existing harness logic.
//        /// </summary>
//        async static Task<int> RunExperiment(Dictionary<string, object> parameters)
//        {
//            ParameterSweeper.ApplyParameters(parameters);
//            Penguin.PersistentPath = null;
//            Penguin.PersistentTarget = null;
//            Penguin.VisitedCounts.Clear();

//            KillZombieBots();

//            var host = BuildZooscapeHost();

//            try
//            {
//                Log.Information("[Harness] Starting host...");
//                await host.StartAsync();
//                await Task.Delay(2000);

//                var omNomNomTask = StartOmNomNomDebug();

//                StartAnimalBot("Parrot", "P1");
//                StartAnimalBot("Penguin", "P2");
//                StartAnimalBot("Snake", "S1");

//                Log.Information("[Harness] Bots started. Awaiting final score...");

//                var timeout = Task.Delay(300000); // 5-minute safety net for the game
//                var completed = await Task.WhenAny(omNomNomTask, timeout);
//                int score = completed == timeout ? -1 : await omNomNomTask;

//                await host.StopAsync();
//                return score;
//            }
//            catch (Exception ex)
//            {
//                Log.Error(ex, "[Harness] RunExperiment failed");
//                try { await host.StopAsync(); } catch { }
//                return -1;
//            }
//        }

//        static void KillZombieBots()
//        {
//            foreach (var proc in Process.GetProcessesByName("NETCoreBot"))
//            {
//                try { proc.Kill(); } catch { }
//            }
//        }

//        static IHost BuildZooscapeHost()
//        {
//            var cloudLog = new SerilogLoggerFactory(Log.Logger).CreateLogger<CloudIntegrationService>();
//            CloudSettings cloudSettings = new();
//            CloudIntegrationService cloudIntegrationService = new(cloudSettings, cloudLog);

//            var host = Host.CreateDefaultBuilder()
//                .ConfigureServices((context, services) =>
//                {
//                    services.AddSingleton<ICloudIntegrationService>(cloudIntegrationService);
//                    services.Configure<SignalRConfigOptions>(context.Configuration.GetSection("SignalR"));
//                    services.AddSignalR(options =>
//                    {
//                        options.EnableDetailedErrors = true;
//                        options.MaximumReceiveMessageSize = 40000000;
//                        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
//                        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
//                    });
//                    services.Configure<GameSettings>(context.Configuration.GetSection("GameSettings"));
//                    services.Configure<S3Configuration>(context.Configuration.GetSection("S3Configuration"));
//                    services.AddKeyedSingleton<IEventDispatcher, SignalREventDispatcher>("signalr");
//                    services.AddKeyedSingleton<IEventDispatcher, CloudEventDispatcher>("cloud");
//                    services.AddKeyedSingleton<IEventDispatcher, LogStateEventDispatcher>("logState");

//                    S3.LogDirectory = Environment.GetEnvironmentVariable("LOG_DIR") ?? Path.Combine(AppContext.BaseDirectory, "logs");

//                    services.AddSingleton<IStreamingFileLogger>(new StreamingFileLogger(S3.LogDirectory, "gameLogs.log"));
//                    services.AddSingleton<IZookeeperService, ZookeeperService>();
//                    services.AddTransient<BotHub>();
//                    services.AddSingleton<IGameStateService, GameStateService>();
//                    services.AddHostedService<WorkerService>();
//                    services.AddSingleton<GameVisualizerService>(provider =>
//                    {
//                        var gamestateService = provider.GetRequiredService<IGameStateService>();
//                        var hubContext = provider.GetRequiredService<IHubContext<BotHub>>();
//                        return new GameVisualizerService(gamestateService, hubContext);
//                    });
//                    services.AddHostedService<GameStateRenderingService>();
//                })
//                .ConfigureWebHostDefaults(webBuilder =>
//                {
//                    webBuilder.UseUrls("http://*:5433");
//                    webBuilder.Configure(app =>
//                    {
//                        app.UseRouting();
//                        app.UseStaticFiles();
//                        app.UseEndpoints(endpoints =>
//                        {
//                            endpoints.MapHub<BotHub>("/bothub");
//                            endpoints.MapFallbackToFile("index.html");
//                        });
//                    });
//                })
//                .UseSerilog((context, services, serilogConfig) =>
//                    serilogConfig.ReadFrom.Configuration(context.Configuration))
//                .Build();

//            return host;
//        }

//        static void StartAnimalBot(string animal, string nick)
//        {
//            string referenceBotPath = $"C:\\Users\\Kevin Olckers\\Documents\\GitHub\\2025-Zooscape\\ZooAnimals\\{animal}\\NETCoreBot.exe";
//            if (!File.Exists(referenceBotPath)) return;

//            var token = Guid.NewGuid();
//            var startInfo = new ProcessStartInfo
//            {
//                FileName = referenceBotPath,
//                Arguments = $"--nickname {nick} --token {token}",
//                UseShellExecute = false,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                CreateNoWindow = true,
//                WorkingDirectory = Path.GetDirectoryName(referenceBotPath)
//            };
//            var process = new Process { StartInfo = startInfo };
//            process.Start();
//            process.BeginOutputReadLine();
//            process.BeginErrorReadLine();
//        }

//        static async Task<int> StartOmNomNomDebug()
//        {
//            var url = "http://localhost:5433/bothub";
//            var nickname = "OmNomNom";
//            var token = Guid.NewGuid().ToString();

//            var connection = new HubConnectionBuilder()
//                .WithUrl($"{url}")
//                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug))
//                .WithAutomaticReconnect()
//                .Build();

//            var botService = new BotService();
//            BotCommand botCommand = new BotCommand();
//            int finalScore = 0;
//            var gameFinished = new TaskCompletionSource<int>();

//            connection.On<Guid>("Registered", id => botService.SetBotId(id));

//            connection.On<GameState>("GameState", async gamestate =>
//            {
//                botCommand = botService.ProcessState(gamestate);
//                var myBot = gamestate.Animals.FirstOrDefault(a => a.Id == botService.GetBotId());
//                if (myBot != null)
//                {
//                    //|| Cobra.GAME_STAGE == GameStage.LateGame
//                    finalScore = myBot.Score;
//                    if (gamestate.Cells.Count(c => c.Content == CellContent.Pellet) <= 5 )
//                    {
//                        await connection.StopAsync();
//                        gameFinished.TrySetResult(finalScore);
//                    }
//                }
//            });

//            connection.On<string>("Disconnect", async reason =>
//            {
//                await connection.StopAsync();
//                gameFinished.TrySetResult(finalScore);
//            });

//            connection.Closed += async error =>
//            {
//                gameFinished.TrySetResult(finalScore);
//                await Task.CompletedTask;
//            };

//            await connection.StartAsync();
//            await connection.InvokeAsync("Register", token, nickname);

//            _ = Task.Run(async () =>
//            {
//                while (connection.State == HubConnectionState.Connected || connection.State == HubConnectionState.Connecting)
//                {
//                    if (botCommand == null || botCommand.Action < BotAction.Up || botCommand.Action > BotAction.Right)
//                    {
//                        await Task.Delay(20);
//                        continue;
//                    }
//                    await connection.SendAsync("BotCommand", botCommand);
//                    botCommand = null;
//                }
//            });

//            return await gameFinished.Task;
//        }

//        /// <summary>
//        /// Quick noise test: 5 random configs ×10 runs each,
//        /// decide between (iter=100, runs=5) vs (iter=50, runs=10).
//        /// </summary>
//        public static async Task RunSigmaTestAsync()
//        {
//            const int testConfigs = 5;
//            const int runsPerConfig = 10;
//            var ratios = new List<double>();

//            for (int i = 0; i < testConfigs; i++)
//            {
//                // pick a random combo
//                var parameters = ParameterSweeper.GenerateRandomParameter();

//                // collect scores
//                var scores = new List<double>();
//                for (int j = 0; j < runsPerConfig; j++)
//                {
//                    int score = await RunExperiment(parameters);
//                    scores.Add(score);
//                }

//                // compute mean and standard deviation
//                double mean = scores.Average();
//                double variance = scores.Select(s => (s - mean) * (s - mean)).Average();
//                double sigma = Math.Sqrt(variance);
//                double ratio = sigma / mean;

//                Console.WriteLine(
//                    $"Config {i + 1}: mean={mean:F1}, σ={sigma:F1}, σ/mean={ratio:F2}"
//                );
//                ratios.Add(ratio);
//            }

//            double avgRatio = ratios.Average();
//            Console.WriteLine($"\nAverage σ/mean = {avgRatio:F2}");

//            if (avgRatio < 0.15)
//            {
//                Console.WriteLine(
//                    "Low noise detected → use Option A: iterFactor=100, runsPerCombo=5"
//                );
//            }
//            else
//            {
//                Console.WriteLine(
//                    "High noise detected → use Option B: iterFactor=50, runsPerCombo=10"
//                );
//            }
//        }

//        // surrogate-model data class
//        public class ParamEval
//        {
//            public float Alpha { get; set; }
//            public float VisitedTilePenalty { get; set; }
//            public float ReversingPenalty { get; set; }
//            public float ZookeeperAvoidance { get; set; }
//            public float EnemyPathAvoidance { get; set; }
//            public float PelletBonus { get; set; }
//            public float DangerThreshold { get; set; }
//            public float MaxPelletCandidates { get; set; }
//            public float CorridorPenalty { get; set; }

//            [ColumnName("Label")]
//            public double Score { get; set; }

//            public ParamEval() { }
//            public ParamEval(Dictionary<string, object> p, double score)
//            {
//                Alpha = Convert.ToSingle(p["ALPHA"]);
//                VisitedTilePenalty = Convert.ToSingle(p["VISITED_TILE_PENALTY_FACTOR"]);
//                ReversingPenalty = Convert.ToSingle(p["REVERSING_TO_PARENT_PENALTY"]);
//                ZookeeperAvoidance = Convert.ToSingle(p["ZOOKEEPER_AVOIDANCE_FACTOR"]);
//                EnemyPathAvoidance = Convert.ToSingle(p["ENEMY_PATH_AVOIDANCE"]);
//                PelletBonus = Convert.ToSingle(p["PELLET_BONUS"]);
//                DangerThreshold = Convert.ToSingle(p["DANGER_THRESHOLD"]);
//                MaxPelletCandidates = Convert.ToSingle(p["MAX_PELLET_CANDIDATES"]);
//                CorridorPenalty = Convert.ToSingle(p["CORRIDOR_PENALTY"]);
//                Score = score;
//            }

//            /// <summary>
//            /// Unique string key for duplicate checking.
//            /// </summary>
//            public string UniqueKey =>
//                $"{Alpha}-{VisitedTilePenalty}-{ReversingPenalty}-" +
//                $"{ZookeeperAvoidance}-{EnemyPathAvoidance}-{PelletBonus}-" +
//                $"{DangerThreshold}-{MaxPelletCandidates}-{CorridorPenalty}";

//            /// <summary>
//            /// Convert back to parameter dictionary.
//            /// </summary>
//            public Dictionary<string, object> ToDictionary() =>
//                new Dictionary<string, object>
//                {
//                    ["ALPHA"] = Alpha,
//                    ["VISITED_TILE_PENALTY_FACTOR"] = (int)VisitedTilePenalty,
//                    ["REVERSING_TO_PARENT_PENALTY"] = (int)ReversingPenalty,
//                    ["ZOOKEEPER_AVOIDANCE_FACTOR"] = ZookeeperAvoidance,
//                    ["ENEMY_PATH_AVOIDANCE"] = (int)EnemyPathAvoidance,
//                    ["PELLET_BONUS"] = (int)PelletBonus,
//                    ["DANGER_THRESHOLD"] = (int)DangerThreshold,
//                    ["MAX_PELLET_CANDIDATES"] = (int)MaxPelletCandidates,
//                    ["CORRIDOR_PENALTY"] = (int)CorridorPenalty
//                };
//        }

//        public class Pred
//        {
//            [ColumnName("Score")]
//            public double Score { get; set; }
//        }
//    }
//}
