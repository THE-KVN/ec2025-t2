using NETCoreBot.Enums;
using NETCoreBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace NETCoreBot.Strategy
{
    public static class LabRat
    {
        #region Variables
        // --- Portal/Map Configuration ---
        public static int MapWidth = 30;  // Replaced at runtime
        public static int MapHeight = 20; // Replaced at runtime
        public static bool PortalLeft = true;
        public static bool PortalRight = true;
        public static bool PortalUp = true;
        public static bool PortalDown = true;
        public static int lastTickCommandIssued = 0;
        public static Cell? PersistentTarget { get; set; } = null;
        public static List<LabRatNode>? PersistentPath { get; set; } = null;



        public static Dictionary<(int, int), int> VisitedCounts { get; set; } = new Dictionary<(int, int), int>();
        public static readonly Queue<(int, int)> RecentPositions = new Queue<(int, int)>();
        public static int stuckCounter = 0;
        public static readonly Random rng = new Random();
        public static BotAction? LastMove { get; set; } = null;
        public static Cell? CurrentTargetPellet = null;
        public static bool IsInDanger = false;

        // Execution time tracking
        public static int ExecutionTimeExceedCount { get; private set; } = 0;
        public static long TotalExecutionTime { get; set; } = 0;
        public static int TickCount { get; set; } = 0;
        public static double AverageExecutionTime => TickCount > 0 ? (double)TotalExecutionTime / TickCount : 0;
        public static long minExecutionTime = long.MaxValue;
        public static long maxExecutionTime = 0;
        public static long LowestExecutionTime => TickCount > 0 ? minExecutionTime : 0;
        public static long HighestExecutionTime => maxExecutionTime;
        public static int currentDeathCounter = 0;

        public static bool IGNORE_REPLAN = false;
        public static HashSet<(int, int)> NarrowPelletAvoidanceSet = new();
        public static HashSet<(int, int)> ContestedPelletsThisTick = new();

        public static int breakoutCounter = 0;

        #endregion

        #region Game Stage Settings

        public static GameStage GAME_STAGE = GameStage.EarlyGame;

        // Tweakable parameters
        public static double ALPHA = 1.3;
        public static int VISITED_TILE_PENALTY_FACTOR = 2;
        public static double REVERSAL_RANDOM_SKIP_CHANCE = 0.2;
        public static int REVERSING_TO_PARENT_PENALTY = 5;
        public static double ZOOKEEPER_AVOIDANCE_FACTOR = 12;
        public static int ENEMY_PATH_AVOIDANCE = 4;
        public static int PELLET_BONUS = 9;
        public static int DANGER_THRESHOLD = 200;

        //Early Game
        public static double EG_ALPHA = 1.3;
        public static int EG_VISITED_TILE_PENALTY_FACTOR = 2;
        public static double EG_REVERSAL_RANDOM_SKIP_CHANCE = 0.2;
        public static int EG_REVERSING_TO_PARENT_PENALTY = 5;
        public static double EG_ZOOKEEPER_AVOIDANCE_FACTOR = 12;
        public static int EG_ENEMY_PATH_AVOIDANCE = 4;
        public static int EG_PELLET_BONUS = 9;
        public static int EG_DANGER_THRESHOLD = 200;

        //Late Game
        public static double LG_ALPHA = 1.3;
        public static int LG_VISITED_TILE_PENALTY_FACTOR = 2;
        public static double LG_REVERSAL_RANDOM_SKIP_CHANCE = 0.2;
        public static int LG_REVERSING_TO_PARENT_PENALTY = 5;
        public static double LG_ZOOKEEPER_AVOIDANCE_FACTOR = 12;
        public static int LG_ENEMY_PATH_AVOIDANCE = 4;
        public static int LG_PELLET_BONUS = 9;
        public static int lG_DANGER_THRESHOLD = 10000;

        #endregion

        #region Constants

        public const int SAFE_DISTANCE = 5;
        public const int STUCK_THRESHOLD = 3;
        public const int RECENT_POS_QUEUE_SIZE = 5;
        public const int REPLAN_INTERVAL = 5;

        #endregion


        //TODO
        public const int CORRIDOR_PENALTY = 25;
        

         
       
        public static BotCommand? ProcessState(GameState gameStateDTO, Guid id)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx | START | xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
            try
            {
                if (gameStateDTO.Tick <= 2)
                {
                    ApplyParameters(GAME_STAGE);
                }

                if (gameStateDTO.Tick > 100 && GAME_STAGE != GameStage.MidGame)
                {
                    GAME_STAGE = GameStage.MidGame;
                    ApplyParameters(GAME_STAGE);
                }


                 return CollectPellets(gameStateDTO, id);
            }
            finally
            {
                // Execution time tracking
                stopwatch.Stop();
                long elapsed = stopwatch.ElapsedMilliseconds;
                TotalExecutionTime += elapsed;
                TickCount++;

                if (elapsed > 200)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Time : {elapsed}");
                    Console.ForegroundColor = ConsoleColor.White;
                    ExecutionTimeExceedCount++;
                }
                else
                {                
                    Console.WriteLine($"Time : {elapsed}");
                }

                if (elapsed < minExecutionTime) minExecutionTime = elapsed;
                if (elapsed > maxExecutionTime) maxExecutionTime = elapsed;

                Console.WriteLine($"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx | END | xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

            }
        }
        public static void ApplyParameters(GameStage stage)
        {
            switch (stage)
            {
                case GameStage.EarlyGame:
                    LabRat.ALPHA = EG_ALPHA;
                    LabRat.VISITED_TILE_PENALTY_FACTOR = EG_VISITED_TILE_PENALTY_FACTOR;
                    LabRat.REVERSAL_RANDOM_SKIP_CHANCE = EG_REVERSAL_RANDOM_SKIP_CHANCE;
                    LabRat.REVERSING_TO_PARENT_PENALTY = EG_REVERSING_TO_PARENT_PENALTY;
                    LabRat.ZOOKEEPER_AVOIDANCE_FACTOR = EG_ZOOKEEPER_AVOIDANCE_FACTOR;
                    LabRat.ENEMY_PATH_AVOIDANCE = EG_ENEMY_PATH_AVOIDANCE;
                    LabRat.PELLET_BONUS = EG_PELLET_BONUS;
                    LabRat.DANGER_THRESHOLD = EG_DANGER_THRESHOLD;
                    break;
                case GameStage.MidGame:
                    LabRat.ALPHA = LG_ALPHA;
                    LabRat.VISITED_TILE_PENALTY_FACTOR = LG_VISITED_TILE_PENALTY_FACTOR;
                    LabRat.REVERSAL_RANDOM_SKIP_CHANCE = LG_REVERSAL_RANDOM_SKIP_CHANCE;
                    LabRat.REVERSING_TO_PARENT_PENALTY = LG_REVERSING_TO_PARENT_PENALTY;
                    LabRat.ZOOKEEPER_AVOIDANCE_FACTOR = LG_ZOOKEEPER_AVOIDANCE_FACTOR;
                    LabRat.ENEMY_PATH_AVOIDANCE = LG_ENEMY_PATH_AVOIDANCE;
                    LabRat.PELLET_BONUS = LG_PELLET_BONUS;
                    LabRat.DANGER_THRESHOLD = lG_DANGER_THRESHOLD;
                    break;
                case GameStage.LateGame:
                    break;
                default:
                    break;
            }
        }
        public static BotCommand? CollectPellets(GameState gameStateDTO, Guid id)
        {

            if (gameStateDTO.Tick == lastTickCommandIssued)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Tick mismatch | Game State {gameStateDTO.Tick} | Last Tick Command {lastTickCommandIssued}");
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }

            var myAnimal = gameStateDTO.Animals.FirstOrDefault(x => x.Id == id);

            if (myAnimal == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Cannot find my animal");
                Console.ForegroundColor = ConsoleColor.White;
                lastTickCommandIssued = gameStateDTO.Tick;
                return null;
            }

            Console.WriteLine($"Current Location: ({myAnimal.X}, {myAnimal.Y})");

            bool onTopOfAnimal = gameStateDTO.Animals.Where(x=> x.Id != myAnimal.Id).Any(x => x.X == myAnimal.X && x.Y == myAnimal.Y);
            if (onTopOfAnimal)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("### ON TOP OF ANIMAL #####");
                Console.ForegroundColor = ConsoleColor.White;

                breakoutCounter++;

                if (breakoutCounter > 5)
                {
                    Console.WriteLine("Breakout detected, clearing path and target.");
                    PersistentPath = null;
                    PersistentTarget = null;
                    breakoutCounter = 0;
                }
                else
                {
                    return new BotCommand { Action = LastMove.Value };
                }
            }
            else
            {
                breakoutCounter = 0;
            }


                // Update map dimensions
                MapWidth = gameStateDTO.Cells.Max(c => c.X) + 1;
            MapHeight = gameStateDTO.Cells.Max(c => c.Y) + 1;

            // --- Respawn Safety Check ---
            //If the animal is at its spawn, check if any zookeeper is too close.
            if (((currentDeathCounter < myAnimal.CapturedCounter) || myAnimal.IsViable == false) && gameStateDTO.Tick > 5)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("### RESPAWNING #####");

                currentDeathCounter = myAnimal.CapturedCounter;
                if (!IsSafeToLeaveSpawn(myAnimal, gameStateDTO, SAFE_DISTANCE))
                {
                    Console.WriteLine("Zookeeper too close to spawn. Waiting to leave spawn.");
                    lastTickCommandIssued = gameStateDTO.Tick;

                    Console.WriteLine($"Tick: {lastTickCommandIssued} | Waiting");
                    return null;
                }
            }

            Console.ForegroundColor = ConsoleColor.White;

            if (GAME_STAGE == GameStage.MidGame)
            {
                if (IsZookeeperNear(myAnimal, gameStateDTO))
                {
                    if (HasPortals(gameStateDTO))
                    {
                        PersistentPath = null;
                        PersistentTarget = null;
                        IGNORE_REPLAN = true;
                        IsInDanger = true;


                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("##### DANGER DANGER DANGER DANGER DANGER #################################");
                        Console.ForegroundColor = ConsoleColor.White;

                        var esacpeAction = EscapeZookeeper(gameStateDTO, myAnimal);

                        if (esacpeAction != null)
                        {
                            //if (LastMove.HasValue && esacpeAction.Action == LastMove.Value)
                            //{
                            //    Console.WriteLine($"Keep going {LastMove.Value}");
                            //    lastTickCommandIssued = gameStateDTO.Tick;
                            //    return null; ;
                            //}
                            //else
                            //{
                            LastMove = esacpeAction.Action;

                            lastTickCommandIssued = gameStateDTO.Tick;
                            return esacpeAction;
                            //}
                        }
                    }
                    else
                    {
                        PersistentPath = null;
                        PersistentTarget = null;
                        IGNORE_REPLAN = true;
                        IsInDanger = true;

                        //addd escape logic here
                        // --- New Chased Escape Logic (no portals available) ---
                        var escapeAction = EscapeChasedZookeeper(gameStateDTO, myAnimal);
                        if (escapeAction != null)
                        {
                            //if (LastMove.HasValue && escapeAction.Action == LastMove.Value)
                            //{
                            //    Console.WriteLine($"Keep going {LastMove.Value}");
                            //    lastTickCommandIssued = gameStateDTO.Tick;
                            //    return null; 
                            //}
                            //else
                            //{

                            LastMove = escapeAction.Action;

                            lastTickCommandIssued = gameStateDTO.Tick;
                            return escapeAction;
                            //}
                        }
                    }
                }
            }


            IGNORE_REPLAN = false;
            IsInDanger = false;
            Console.ForegroundColor = ConsoleColor.White;


            //if (PersistentTarget != null && ContestedPelletsThisTick.Contains((PersistentTarget.X, PersistentTarget.Y)))
            //{
            //    Console.ForegroundColor = ConsoleColor.DarkCyan;
            //    Console.WriteLine("##### SAME TARGET SAME TARGET SAME TARGET #################################");
            //    Console.ForegroundColor = ConsoleColor.White;
            //}


            // Update visited counts
            var currentPos = (myAnimal.X, myAnimal.Y);
            if (!VisitedCounts.ContainsKey(currentPos))
                VisitedCounts[currentPos] = 0;
            VisitedCounts[currentPos]++;

            // Basic stuck detection
            if (RecentPositions.Contains(currentPos))
            {
                stuckCounter++;
                if (stuckCounter >= STUCK_THRESHOLD && !IGNORE_REPLAN)
                {
                    Console.WriteLine("Stuck threshold reached; clearing path & target.");
                    PersistentPath = null;
                    PersistentTarget = null;
                    stuckCounter = 0;

                    var tieBreakTarget = FindNearestPelletTieBreak(myAnimal.X, myAnimal.Y, gameStateDTO.Cells, LastMove);
                    if (tieBreakTarget != null)
                    {
                        Console.WriteLine($"New target acquired (break out of loop fallback): ({tieBreakTarget.X}, {tieBreakTarget.Y})");
                        PersistentTarget = tieBreakTarget;
                        PersistentPath = null;
                    }
                    else
                    {
                        PersistentPath = null;
                        PersistentTarget = null;
                    }

                }
            }
            else
            {
                stuckCounter = 0;
            }
            RecentPositions.Enqueue(currentPos);
            if (RecentPositions.Count > RECENT_POS_QUEUE_SIZE)
                RecentPositions.Dequeue();

            // Periodic partial re-plan check

            if (!IsPathStillValid(PersistentPath, myAnimal, gameStateDTO.Cells, gameStateDTO))
            {
                Console.WriteLine("Periodic check: path invalid => clearing path & target.");
                PersistentPath = null;
                PersistentTarget = null;
    
            }
            if ((gameStateDTO.Tick % REPLAN_INTERVAL == 0) && !IGNORE_REPLAN)
            {
                PersistentPath = null;
                PersistentTarget = null;
    
            }


            // Validate or choose new target
            var target = ValidateOrFindTarget(myAnimal, gameStateDTO);
            CurrentTargetPellet = target;
            if (target == null)
            {
                lastTickCommandIssued = gameStateDTO.Tick;
                return null;
            }

            // Build grid
            var grid = gameStateDTO.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);

            // Compute or reuse path; note the additional parameters passed
            var path = ValidateOrComputePath(myAnimal, target, grid, gameStateDTO);
            if (path == null || path.Count == 0)
            {
                Console.WriteLine("No path found or path is empty. Skipping target.");
                PersistentTarget = null;
                PersistentPath = null;
                CurrentTargetPellet = null;
                lastTickCommandIssued = gameStateDTO.Tick;
                return null;
            }

            // Rate-limited move issuance
            BotAction? action = ComputeNextMoveRateLimited(myAnimal, path);
            if (!action.HasValue)
            {
                Console.WriteLine("Rate-limited: Not issuing a new command this tick.");
                lastTickCommandIssued = gameStateDTO.Tick;
                return null;
            }


            PersistentPath?.RemoveAt(0);

            // If target reached, clear persistent state
            if (myAnimal.X == target.X && myAnimal.Y == target.Y)
            {
                Console.WriteLine("Target reached. Clearing persistent data.");
                PersistentTarget = null;
                PersistentPath = null;
            }

            //if (LastMove.HasValue && action.Value == LastMove.Value)
            //{
            //    Console.WriteLine($"Keep going {LastMove.Value}");


            //    lastTickCommandIssued = gameStateDTO.Tick;
            //    return null; ;
            //}
            //else
            //{

            Console.WriteLine($"Tick: {gameStateDTO.Tick} | Moving {action.Value} | Score {myAnimal.Score}");
            LastMove = action.Value;

            lastTickCommandIssued = gameStateDTO.Tick;
            return new BotCommand { Action = action.Value };
            //}
        }




        public static BotCommand? EscapeChasedZookeeper(GameState state, Animal myAnimal)
        {
            // 1. Identify the closest zookeeper.
            Zookeeper? closestZookeeper = null;
            int minDistance = int.MaxValue;
            foreach (var zk in state.Zookeepers)
            {
                int d = Manhattan(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (d < minDistance)
                {
                    minDistance = d;
                    closestZookeeper = zk;
                }
            }
            if (closestZookeeper == null)
                return null;

            // Estimate the value of escaping vs. dying
            int ticksToEscape = 30;
            double averagePelletsPerTick = 0.4; // Tweak this based on testing/data
            double estimatedPelletsLost = ticksToEscape * averagePelletsPerTick;
            double pelletLossIfCaught = myAnimal.Score * 0.10;

            Console.WriteLine($"Escape decision -> TicksToEscape: {ticksToEscape}, EstPelletsLost: {estimatedPelletsLost:F1}, OnDeathLoss: {pelletLossIfCaught:F1}");

            if (pelletLossIfCaught < estimatedPelletsLost)
            {
                Console.WriteLine("Not worth escaping - better to take the hit.");
                return null; // Don't escape, let the normal logic handle it
            }



            // 2. Compute primary escape move.
            int dx = myAnimal.X - closestZookeeper.X;
            int dy = myAnimal.Y - closestZookeeper.Y;
            BotAction primaryAction = Math.Abs(dx) >= Math.Abs(dy)
                ? (dx >= 0 ? BotAction.Right : BotAction.Left)
                : (dy >= 0 ? BotAction.Down : BotAction.Up);

            int newX = myAnimal.X, newY = myAnimal.Y;
            switch (primaryAction)
            {
                case BotAction.Up:
                    newY = myAnimal.Y - 1;
                    break;
                case BotAction.Down:
                    newY = myAnimal.Y + 1;
                    break;
                case BotAction.Left:
                    newX = myAnimal.X - 1;
                    break;
                case BotAction.Right:
                    newX = myAnimal.X + 1;
                    break;
            }

            // 3. Check if primary move is valid.
            if (newX >= 0 && newX < MapWidth && newY >= 0 && newY < MapHeight)
            {
                var cell = state.Cells.FirstOrDefault(c => c.X == newX && c.Y == newY);
                if (cell != null && cell.Content != CellContent.Wall &&
                    cell.Content != CellContent.AnimalSpawn && cell.Content != CellContent.ZookeeperSpawn)
                {
                    Console.WriteLine($"Chased escape: moving {primaryAction}");
                    return new BotCommand { Action = primaryAction };
                }
            }

            // 4. Fallback: Evaluate all valid directions.
            BotAction? bestAction = null;
            int bestDistance = minDistance;
            foreach (BotAction action in new[] { BotAction.Up, BotAction.Down, BotAction.Left, BotAction.Right })
            {
                int altX = myAnimal.X, altY = myAnimal.Y;
                switch (action)
                {
                    case BotAction.Up:
                        altY = myAnimal.Y - 1;
                        break;
                    case BotAction.Down:
                        altY = myAnimal.Y + 1;
                        break;
                    case BotAction.Left:
                        altX = myAnimal.X - 1;
                        break;
                    case BotAction.Right:
                        altX = myAnimal.X + 1;
                        break;
                }
                if (altX < 0 || altX >= MapWidth || altY < 0 || altY >= MapHeight)
                    continue;
                var altCell = state.Cells.FirstOrDefault(c => c.X == altX && c.Y == altY);
                if (altCell == null || altCell.Content == CellContent.Wall ||
                    altCell.Content == CellContent.AnimalSpawn || altCell.Content == CellContent.ZookeeperSpawn)
                    continue;
                int newDist = Manhattan(altX, altY, closestZookeeper.X, closestZookeeper.Y);
                if (newDist > bestDistance)
                {
                    bestDistance = newDist;
                    bestAction = action;
                }
            }
            if (bestAction != null)
            {
                Console.WriteLine($"Chased escape alternative: moving {bestAction}");
                return new BotCommand { Action = bestAction.Value };
            }
            return null;
        }

        public static bool IsSafeToLeaveSpawn(Animal myAnimal, GameState state, int safeDistance)
        {
            foreach (var zk in state.Zookeepers)
            {
                // Compute Manhattan distance between animal and zookeeper
                int distance = Manhattan(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (distance < safeDistance)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsPathStillValid(List<LabRatNode>? path, Animal myAnimal, List<Cell> allCells, GameState state)
        {
            // Check that the path exists and that our animal is at the start of the path.
            if (path == null || path.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("PATH INVALID: Path is null or empty.");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
            
            var firstNode = path[0];
            if ((myAnimal.X != firstNode.X || myAnimal.Y != firstNode.Y) && myAnimal.IsViable)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"PATH INVALID: Animal out of sync. Animal at ({myAnimal.X}, {myAnimal.Y}) | Path[0] at ({firstNode.X}, {firstNode.Y}) | Animal viable {myAnimal.IsViable}");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            // Build a grid for cell lookups.
            var grid = allCells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            foreach (var node in path)
            {
                if (!grid.TryGetValue((node.X, node.Y), out var content))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"PATH INVALID: Cannot get content at ({node.X}, {node.Y}): {content}");
                    Console.ForegroundColor = ConsoleColor.White;
                    return false;
                }
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"PATH INVALID: Invalid content at ({node.X}, {node.Y}): {content}");
                    Console.ForegroundColor = ConsoleColor.White;
                    return false;
                }
                   
            }

            // Compute the danger along the path.
            int totalDanger = 0;
            foreach (var node in path)
            {
                // Compute the zookeeper penalty at this node.
                // (Assuming ComputeZookeeperPenalty is defined elsewhere and uses state.Zookeepers)
                totalDanger += ComputeZookeeperPenalty(node.X, node.Y, myAnimal, state);

                if (totalDanger > DANGER_THRESHOLD)
                {
                    // Path is too dangerous.
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"PATH INVALID: Path is too dangerous Total danger is {totalDanger} and danger threshold is {DANGER_THRESHOLD}");
                    Console.ForegroundColor = ConsoleColor.White;
                    return false;
                }

            }

            // Define a threshold for path danger (tune this value as needed).

            

            return true;
        }

        public static Cell? ValidateOrFindTarget(Animal myAnimal, GameState state)
        {
            if (PersistentTarget != null)
            {
                bool stillPellet = state.Cells.Any(c =>
                    c.X == PersistentTarget.X &&
                    c.Y == PersistentTarget.Y &&
                    c.Content == CellContent.Pellet);
                if (stillPellet)
                {
                    Console.WriteLine($"Using persistent target: ({PersistentTarget.X}, {PersistentTarget.Y})");
                    return PersistentTarget;
                }
            }
            var clusterTarget = FindPelletInLargestConnectedClusterWeighted(myAnimal, state.Cells, ALPHA);
            if (clusterTarget != null)
            {
                Console.WriteLine($"[ClusterMapping] Target from BFS approach: ({clusterTarget.X}, {clusterTarget.Y})");
                PersistentTarget = clusterTarget;
                PersistentPath = null;
                return clusterTarget;
            }
            var tieBreakTarget = FindNearestPelletTieBreak(myAnimal.X, myAnimal.Y, state.Cells, LastMove);
            if (tieBreakTarget != null)
            {
                Console.WriteLine($"New target acquired (tie-break fallback): ({tieBreakTarget.X}, {tieBreakTarget.Y})");
                PersistentTarget = tieBreakTarget;
                PersistentPath = null;
            }
            return tieBreakTarget;
        }

        public static List<LabRatNode>? ValidateOrComputePath(Animal myAnimal, Cell target, Dictionary<(int, int), CellContent> grid, GameState state)
        {
            if (PersistentPath != null && PersistentPath.Count > 0)
            {
                Console.WriteLine("Using persistent path.");
                LogCurrentPath(PersistentPath);
                return PersistentPath;
            }
            var path = FindPath(myAnimal.X, myAnimal.Y, target.X, target.Y, grid, myAnimal, state);
            PersistentPath = path;
            if (path != null && path.Count > 0)
            {
                Console.WriteLine("Computed new path.");
                LogCurrentPath(path);
            }
            return path;
        }

        public static void LogCurrentPath(List<LabRatNode> path)
        {
            var coords = path.Select(n => $"({n.X},{n.Y})");
            Console.WriteLine("Current path: " + string.Join(" -> ", coords));
        }

        public static BotAction? ComputeNextMoveRateLimited(Animal myAnimal, List<LabRatNode> path)
        {
            if (path.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Rate Limited: No path available for next move.");
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }
            var firstStep = path[0];
            if ((myAnimal.X != firstStep.X || myAnimal.Y != firstStep.Y) && myAnimal.IsViable)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Rate Limited: Out of sync | Animal is at ({myAnimal.X}, {myAnimal.Y}) | Path[0] is at ({firstStep.X},{firstStep.Y}) | Is Viable : {myAnimal.IsViable} ");
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }

            if (path.Count < 2)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Rate Limited: Path is too short | Animal is at ({myAnimal.X}, {myAnimal.Y}) | Path[0] is at ({firstStep.X},{firstStep.Y}) | Is Viable : {myAnimal.IsViable} | Last Move: {LastMove}");
                Console.ForegroundColor = ConsoleColor.White;
                return null ;
            }
            var secondStep = path[1];
            var action = GetDirection(firstStep.X, firstStep.Y, secondStep.X, secondStep.Y);
            //if (!IGNORE_REPLAN && action.HasValue && LastMove.HasValue && action.Value == Opposite(LastMove.Value))
            //{
            //    Console.WriteLine("Detected immediate reversal, check if its stuck");
            //    if (path.Count > 2)
            //    {
            //        var thirdStep = path[2];
            //        var altAction = GetDirection(secondStep.X, secondStep.Y, thirdStep.X, thirdStep.Y);
            //        if (altAction.HasValue && altAction.Value != Opposite(LastMove.Value))
            //        {
            //            Console.WriteLine("Using alternative to avoid reversal. - is at portal i think");
            //            path.RemoveAt(1);
            //            return altAction;
            //        }
            //    }
            //    //if (rng.NextDouble() < REVERSAL_RANDOM_SKIP_CHANCE)
            //    //{
            //    //    Console.ForegroundColor = ConsoleColor.Blue;
            //    //    Console.WriteLine($"Rate Limited: Injecting random skip to break tie.");
            //    //    Console.ForegroundColor = ConsoleColor.White;
            //    //    return null;
            //    //}
            //}
            return action;
        }

        public static List<LabRatNode>? FindPath(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid,
            Animal myAnimal,
            GameState state)
        {

            if (GAME_STAGE == GameStage.MidGame)
            {
                NarrowPelletAvoidanceSet = IdentifyNarrowAndLeadInPellets(grid);
                ContestedPelletsThisTick = PredictContestedPellets(myAnimal, state);
            }


            var openSet = new List<LabRatNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new LabRatNode(
                startX, startY,
                0,
                Manhattan(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY, state)
            );
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) =>
                {
                    int cmp = a.F.CompareTo(b.F);
                    if (cmp == 0)
                    {
                        cmp = a.H.CompareTo(b.H);
                        if (cmp == 0)
                        {
                            cmp = a.TieBreak.CompareTo(b.TieBreak);
                        }
                    }
                    return cmp;
                });

                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.X == targetX && current.Y == targetY)
                    return ReconstructPath(current);

                closedSet.Add((current.X, current.Y));

                foreach (var neighbor in GetNeighborsPortal(current, grid, targetX, targetY, myAnimal, state))
                {
                    if (closedSet.Contains((neighbor.X, neighbor.Y)))
                        continue;

                    var existing = openSet.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);
                    if (existing != null && existing.G <= neighbor.G)
                        continue;

                    openSet.Add(neighbor);
                }
            }
            return null;
        }
        public static List<LabRatNode>? FindPathEscape(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid,
            Animal myAnimal,
            GameState state)
        {

            var openSet = new List<LabRatNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new LabRatNode(
                startX, startY,
                0,
                Manhattan(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY, state)
            );
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) =>
                {
                    int cmp = a.F.CompareTo(b.F);
                    if (cmp == 0)
                    {
                        cmp = a.H.CompareTo(b.H);
                        if (cmp == 0)
                        {
                            cmp = a.TieBreak.CompareTo(b.TieBreak);
                        }
                    }
                    return cmp;
                });

                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.X == targetX && current.Y == targetY)
                    return ReconstructPath(current);

                closedSet.Add((current.X, current.Y));

                foreach (var neighbor in GetNeighborsPortalEscape(current, grid, targetX, targetY, myAnimal, state))
                {
                    if (closedSet.Contains((neighbor.X, neighbor.Y)))
                        continue;

                    var existing = openSet.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);
                    if (existing != null && existing.G <= neighbor.G)
                        continue;

                    openSet.Add(neighbor);
                }
            }
            return null;
        }

        public static List<LabRatNode>? FindPathOpponent(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid,
            Animal someAnimal,
            GameState state)
        {

            var openSet = new List<LabRatNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new LabRatNode(
                startX, startY,
                0,
                Manhattan(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY, state)
            );
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) =>
                {
                    int cmp = a.F.CompareTo(b.F);
                    if (cmp == 0)
                    {
                        cmp = a.H.CompareTo(b.H);
                        if (cmp == 0)
                        {
                            cmp = a.TieBreak.CompareTo(b.TieBreak);
                        }
                    }
                    return cmp;
                });

                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.X == targetX && current.Y == targetY)
                    return ReconstructPath(current);

                closedSet.Add((current.X, current.Y));

                foreach (var neighbor in GetNeighborsPortalOpponent(current, grid, targetX, targetY, someAnimal, state))
                {
                    if (closedSet.Contains((neighbor.X, neighbor.Y)))
                        continue;

                    var existing = openSet.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);
                    if (existing != null && existing.G <= neighbor.G)
                        continue;

                    openSet.Add(neighbor);
                }
            }
            return null;
        }


        public static List<LabRatNode> ReconstructPath(LabRatNode node)
        {
            var path = new List<LabRatNode>();
            while (node != null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        public static IEnumerable<LabRatNode> GetNeighborsPortal(
            LabRatNode current,
            Dictionary<(int, int), CellContent> grid,
            int targetX,
            int targetY,
            Animal myAnimal,
            GameState state)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int nx = current.X + directions[i, 0];
                int ny = current.Y + directions[i, 1];

                // Wrap horizontally
                if (GAME_STAGE == GameStage.MidGame)
                {
                    if (nx < 0)
                    {
                        if (PortalLeft) nx = MapWidth - 1;
                        else continue;
                    }
                    else if (nx >= MapWidth)
                    {
                        if (PortalRight) nx = 0;
                        else continue;
                    }

                    // Wrap vertically
                    if (ny < 0)
                    {
                        if (PortalUp) ny = MapHeight - 1;
                        else continue;
                    }
                    else if (ny >= MapHeight)
                    {
                        if (PortalDown) ny = 0;
                        else continue;
                    }
                }

                if (!grid.TryGetValue((nx, ny), out var content))
                    continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                    continue;

                int additionalPenalty = 0;


                if (current.Parent != null && nx == current.Parent.X && ny == current.Parent.Y)
                    additionalPenalty += REVERSING_TO_PARENT_PENALTY;
                if (VisitedCounts.TryGetValue((nx, ny), out int visitCount))
                    additionalPenalty += visitCount * VISITED_TILE_PENALTY_FACTOR;
                if (NarrowPelletAvoidanceSet.Contains((nx, ny)))
                    additionalPenalty += CORRIDOR_PENALTY;
                if (ContestedPelletsThisTick.Contains((nx, ny)))
                    additionalPenalty += ENEMY_PATH_AVOIDANCE;

                if (GAME_STAGE == GameStage.MidGame)
                {
                    if (NarrowPelletAvoidanceSet.Contains((nx, ny)))
                        additionalPenalty += CORRIDOR_PENALTY;
                    if (ContestedPelletsThisTick.Contains((nx, ny)))
                    {
                        int contestedPenalty = ComputeContestedPenatly(myAnimal, nx, ny, state);
                        additionalPenalty += contestedPenalty;
                    }
                        
                }


                int pelletBonus = (content == CellContent.Pellet) ? PELLET_BONUS : 0;
                int zookeeperPenalty = ComputeZookeeperPenalty(nx, ny, myAnimal, state);

 

                int newG = current.G + 1 + additionalPenalty + zookeeperPenalty - pelletBonus;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new LabRatNode(
                    nx, ny,
                    newG, newH,
                    current,
                    ComputeTieBreaker(nx, ny, state)
                );
            }
        }

        public static int ComputeContestedPenatly(Animal myAnimal, int nx, int ny, GameState state)
        {
            bool otherAnimalColser = false;
            int additonPenalty = 0;
            if (ContestedPelletsThisTick.Contains((nx, ny)))
            {
                int myDist = Manhattan(myAnimal.X, myAnimal.Y, nx, ny);
                foreach (var opp in state.Animals)
                {
                    if (opp.Id == myAnimal.Id || opp.IsViable ==false)
                    {
                        continue;
                    }

                    int oppDist = Manhattan(opp.X, opp.Y, nx, ny);

                    if (oppDist < myDist)
                    {
                        otherAnimalColser = true;
                        
                    }

                    if (otherAnimalColser)
                    {
                        additonPenalty += ENEMY_PATH_AVOIDANCE;
                    }
                }
            }

            return additonPenalty;
        }

        public static IEnumerable<LabRatNode> GetNeighborsPortalEscape(
            LabRatNode current,
            Dictionary<(int, int), CellContent> grid,
            int targetX,
            int targetY,
            Animal myAnimal,
            GameState state)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int nx = current.X + directions[i, 0];
                int ny = current.Y + directions[i, 1];

                // Wrap horizontally
                if (nx < 0)
                {
                    if (PortalLeft) nx = MapWidth - 1;
                    else continue;
                }
                else if (nx >= MapWidth)
                {
                    if (PortalRight) nx = 0;
                    else continue;
                }

                // Wrap vertically
                if (ny < 0)
                {
                    if (PortalUp) ny = MapHeight - 1;
                    else continue;
                }
                else if (ny >= MapHeight)
                {
                    if (PortalDown) ny = 0;
                    else continue;
                }

                if (!grid.TryGetValue((nx, ny), out var content))
                    continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                    continue;

                int additionalPenalty = 0;


                if (current.Parent != null && nx == current.Parent.X && ny == current.Parent.Y)
                    additionalPenalty += REVERSING_TO_PARENT_PENALTY;
                if (VisitedCounts.TryGetValue((nx, ny), out int visitCount))
                    additionalPenalty += visitCount * VISITED_TILE_PENALTY_FACTOR;

                int pelletBonus = (content == CellContent.Pellet) ? PELLET_BONUS : 0;
                int zookeeperPenalty = ComputeZookeeperPenalty(nx, ny, myAnimal, state);

                int newG = current.G + 1 + additionalPenalty + zookeeperPenalty - pelletBonus;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new LabRatNode(
                    nx, ny,
                    newG, newH,
                    current,
                    ComputeTieBreaker(nx, ny, state)
                );
            }
        }

        public static IEnumerable<LabRatNode> GetNeighborsPortalOpponent(
                LabRatNode current,
                Dictionary<(int, int), CellContent> grid,
                int targetX,
                int targetY,
                Animal someAnimal,
                GameState state)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int nx = current.X + directions[i, 0];
                int ny = current.Y + directions[i, 1];

                // Wrap horizontally
                if (nx < 0)
                {
                    if (PortalLeft) nx = MapWidth - 1;
                    else continue;
                }
                else if (nx >= MapWidth)
                {
                    if (PortalRight) nx = 0;
                    else continue;
                }

                // Wrap vertically
                if (ny < 0)
                {
                    if (PortalUp) ny = MapHeight - 1;
                    else continue;
                }
                else if (ny >= MapHeight)
                {
                    if (PortalDown) ny = 0;
                    else continue;
                }

                if (!grid.TryGetValue((nx, ny), out var content))
                    continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                    continue;

                int additionalPenalty = 0;

                int pelletBonus = (content == CellContent.Pellet) ? PELLET_BONUS : 0;
                int zookeeperPenalty = ComputeZookeeperPenalty(nx, ny, someAnimal, state);

                int newG = current.G + 1 + additionalPenalty + zookeeperPenalty - pelletBonus;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new LabRatNode(
                    nx, ny,
                    newG, newH,
                    current,
                    ComputeTieBreaker(nx, ny, state)
                );
            }
        }

        public static int ComputeTieBreaker(int x, int y, GameState state)
        {
            if (GAME_STAGE == GameStage.EarlyGame)
            {
                return 0;
            }


            // Compute Manhattan distance to the nearest zookeeper; higher distance means safer.
            if (state.Zookeepers == null || state.Zookeepers.Count == 0)
                return 0;
            int minDistance = int.MaxValue;
            foreach (var zk in state.Zookeepers)
            {
                int d = Manhattan(x, y, zk.X, zk.Y);
                if (d < minDistance)
                    minDistance = d;
            }
            // Use the negative so that a larger distance (safer) yields a lower tie-breaker value.
            return -minDistance;
        }

        // The penalty increases when a neighbor is close to a zookeeper.
        public static int ComputeZookeeperPenalty(int nx, int ny, Animal myAnimal, GameState state)
        {
            int penalty = 0;
            if (state.Zookeepers != null)
            {
                foreach (var zk in state.Zookeepers)
                {
                    int dist = Manhattan(nx, ny, zk.X, zk.Y);
                    // Avoid division by zero and scale penalty relative to risk (current score * 0.1)
                    penalty += (int)(ZOOKEEPER_AVOIDANCE_FACTOR * (myAnimal.Score * 0.1) / Math.Max(dist, 1));
                }
            }
            return penalty;
        }

        public static int Manhattan(int x, int y, int tx, int ty)
        {
            return Math.Abs(x - tx) + Math.Abs(y - ty);
        }

        public static BotAction? GetDirection(int currentX, int currentY, int nextX, int nextY)
        {

            //0 38
            if (currentX == 0 && nextX == MapWidth - 1) return BotAction.Left;
            if (currentX == MapWidth - 1 && nextX == 0) return BotAction.Right; 

            if (currentY == 0 && nextY == MapHeight - 1) return BotAction.Up;
            if (currentY == MapHeight - 1 && nextY == 0) return BotAction.Down;



            if (nextX > currentX) return BotAction.Right;
            if (nextX < currentX) return BotAction.Left;
            if (nextY > currentY) return BotAction.Down;
            if (nextY < currentY) return BotAction.Up;
            return null;
        }

        public static BotAction Opposite(BotAction action)
        {
            return action switch
            {
                BotAction.Up => BotAction.Down,
                BotAction.Down => BotAction.Up,
                BotAction.Left => BotAction.Right,
                BotAction.Right => BotAction.Left,
                _ => action
            };
        }

        // --- Other existing methods remain unchanged (BFS target selection, etc.) ---
        public static Cell? FindPelletInLargestConnectedClusterWeighted(Animal myAnimal, List<Cell> allCells, double alpha)
        {
            var pelletCells = allCells.Where(c => c.Content == CellContent.Pellet).ToList();
            if (pelletCells.Count == 0)
                return null;
            var grid = allCells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            var visited = new HashSet<(int, int)>();
            var clusters = new List<List<Cell>>();
            foreach (var pcell in pelletCells)
            {
                var pos = (pcell.X, pcell.Y);
                if (!visited.Contains(pos))
                {
                    var cluster = BFSCluster(pos, grid, visited);
                    clusters.Add(cluster);
                }
            }
            double bestScore = double.NegativeInfinity;
            List<Cell>? bestCluster = null;
            foreach (var cluster in clusters)
            {
                if (cluster.Count == 0) continue;
                int sumX = 0, sumY = 0;
                foreach (var c in cluster)
                {
                    sumX += c.X;
                    sumY += c.Y;
                }
                int clusterSize = cluster.Count;
                int avgX = sumX / clusterSize;
                int avgY = sumY / clusterSize;
                int dist = Math.Abs(myAnimal.X - avgX) + Math.Abs(myAnimal.Y - avgY);
                double score = clusterSize - alpha * dist;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCluster = cluster;
                }
            }
            if (bestCluster == null || bestCluster.Count == 0)
                return null;
            var bestPellet = bestCluster.OrderBy(p => Manhattan(myAnimal.X, myAnimal.Y, p.X, p.Y)).First();
            return bestPellet;
        }

        public static List<Cell> BFSCluster((int x, int y) start, Dictionary<(int, int), CellContent> grid, HashSet<(int, int)> visited)
        {
            var cluster = new List<Cell>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cluster.Add(new Cell { X = current.x, Y = current.y, Content = CellContent.Pellet });
                int[,] dirs = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
                for (int i = 0; i < dirs.GetLength(0); i++)
                {
                    int nx = current.x + dirs[i, 0];
                    int ny = current.y + dirs[i, 1];
                    var npos = (nx, ny);
                    if (!visited.Contains(npos) && grid.TryGetValue(npos, out var content))
                    {
                        if (content == CellContent.Pellet)
                        {
                            visited.Add(npos);
                            queue.Enqueue(npos);
                        }
                    }
                }
            }
            return cluster;
        }

        public static Cell? FindNearestPelletTieBreak(int startX, int startY, List<Cell> cells, BotAction? lastMove)
        {
            int bestDistance = int.MaxValue;
            var pellets = new List<Cell>();
            foreach (var c in cells)
            {
                if (c.Content == CellContent.Pellet)
                {
                    int dist = Math.Abs(startX - c.X) + Math.Abs(startY - c.Y);
                    if (dist < bestDistance)
                        bestDistance = dist;
                    pellets.Add(c);
                }
            }
            if (pellets.Count == 0) return null;
            var closest = pellets.Where(p => Math.Abs(startX - p.X) + Math.Abs(startY - p.Y) == bestDistance).ToList();
            if (closest.Count == 0) return null;
            var aligned = closest.Where(p => IsAligned(lastMove, startX, startY, p)).ToList();
            return aligned.Count > 0 ? aligned[0] : closest[0];
        }

        public static bool IsAligned(BotAction? lastMove, int myX, int myY, Cell pellet)
        {
            if (!lastMove.HasValue) return false;
            return lastMove.Value switch
            {
                BotAction.Up => pellet.Y < myY,
                BotAction.Down => pellet.Y > myY,
                BotAction.Left => pellet.X < myX,
                BotAction.Right => pellet.X > myX,
                _ => false,
            };
        }

        public static bool HasPortals(GameState state)
        {
            // Compute the map dimensions from the cells.
            int width = state.Cells.Max(c => c.X) + 1;
            int height = state.Cells.Max(c => c.Y) + 1;

            // A cell is considered passable if it is not a wall.
            // (You may need to adjust this depending on your game's rules.)
            bool leftEdgeOpen = state.Cells.Any(c => c.X == 0 && c.Content != CellContent.Wall);
            bool rightEdgeOpen = state.Cells.Any(c => c.X == width - 1 && c.Content != CellContent.Wall);
            bool topEdgeOpen = state.Cells.Any(c => c.Y == 0 && c.Content != CellContent.Wall);
            bool bottomEdgeOpen = state.Cells.Any(c => c.Y == height - 1 && c.Content != CellContent.Wall);

            // We consider portals available if either the horizontal or vertical edges are open.
            return (leftEdgeOpen && rightEdgeOpen) || (topEdgeOpen && bottomEdgeOpen);
        }

        public static bool IsZookeeperNear(Animal myAnimal, GameState state)
        {
            // Loop through each zookeeper in the game state.
            foreach (var zk in state.Zookeepers)
            {
                int distance = Manhattan(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (distance <= SAFE_DISTANCE)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPortalCell(Cell cell)
        {
            // A cell is on a portal if it is on any edge of the map.
            return (cell.X == 0 || cell.X == MapWidth - 1 || cell.Y == 0 || cell.Y == MapHeight - 1);
        }


        public static BotCommand? EscapeZookeeper(GameState state, Animal myAnimal)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            // 1. Find the closest zookeeper.
            if (state.Zookeepers == null || state.Zookeepers.Count == 0)
                return null;

            Zookeeper closestZookeeper = null;
            int minDistance = int.MaxValue;
            foreach (var zk in state.Zookeepers)
            {
                int d = Manhattan(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (d < minDistance)
                {
                    minDistance = d;
                    closestZookeeper = zk;
                }
            }
            if (closestZookeeper == null)
                return null;

            // 2. Compute relative direction.
            int deltaX = myAnimal.X - closestZookeeper.X;
            int deltaY = myAnimal.Y - closestZookeeper.Y;

            int targetX = myAnimal.X;
            int targetY = myAnimal.Y;

            // Choose horizontal escape if horizontal difference is greater.
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                if (deltaX > 0)
                {
                    // Zookeeper is to the left; use right edge if enabled.
                    targetX = PortalRight ? MapWidth - 1 : myAnimal.X;
                }
                else
                {
                    // Zookeeper is to the right; use left edge if enabled.
                    targetX = PortalLeft ? 0 : myAnimal.X;
                }
                targetY = myAnimal.Y;
            }
            else
            {
                // Vertical escape.
                if (deltaY > 0)
                {
                    // Zookeeper is above; use bottom edge if enabled.
                    targetY = PortalDown ? MapHeight - 1 : myAnimal.Y;
                }
                else
                {
                    // Zookeeper is below; use top edge if enabled.
                    targetY = PortalUp ? 0 : myAnimal.Y;
                }
                targetX = myAnimal.X;
            }

            // 3. Find a portal cell on the chosen edge (filtering using IsPortalCell).
            Cell? portalTarget = null;


            var allPortalCellsOrdered = state.Cells
                    .Where(c => IsPortalCell(c) && c.Content != CellContent.Wall)
                    .OrderBy(c => Manhattan(myAnimal.X, myAnimal.Y, c.X, c.Y))
                    .ToList();

            if (Manhattan(myAnimal.X, myAnimal.Y, allPortalCellsOrdered.FirstOrDefault().X, allPortalCellsOrdered.FirstOrDefault().Y) < SAFE_DISTANCE)
            {
                portalTarget = allPortalCellsOrdered.FirstOrDefault();
            }
            else if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // Horizontal escape: search among cells on the targetX column that are portal cells.
                portalTarget = allPortalCellsOrdered
                    .Where(c => c.X == targetX)
                    .OrderBy(c => Manhattan(myAnimal.X, myAnimal.Y, c.X, c.Y))
                    .FirstOrDefault();
            }
            else
            {
                // Vertical escape: search among cells on the targetY row that are portal cells.
                portalTarget = allPortalCellsOrdered
                    .Where(c => c.Y == targetY)
                    .OrderBy(c => Manhattan(myAnimal.X, myAnimal.Y, c.X, c.Y))
                    .FirstOrDefault();
            }
            if (portalTarget == null)
            {
                Console.WriteLine("No valid portal target found.");
                return null;
            }

            // 4. Compute a path to the portal target.
            var grid = state.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            var path = ValidateOrComputePath(myAnimal, portalTarget, grid, state);
            if (path == null || path.Count == 0)
            {
                Console.WriteLine("No path found to portal.");
                return null;
            }

            // Estimate the value of escaping vs. dying
            int ticksToEscape = path.Count;
            double averagePelletsPerTick = 0.4; // Tweak this based on testing/data
            double estimatedPelletsLost = ticksToEscape * averagePelletsPerTick;
            double pelletLossIfCaught = myAnimal.Score * 0.10;

            Console.WriteLine($"Escape decision -> TicksToEscape: {ticksToEscape}, EstPelletsLost: {estimatedPelletsLost:F1}, OnDeathLoss: {pelletLossIfCaught:F1}");

            if (pelletLossIfCaught < estimatedPelletsLost)
            {
                Console.WriteLine("Not worth escaping - better to take the hit.");
                return null; // Don't escape, let the normal logic handle it
            }

            // 5. If the computed path has less than 2 nodes (we’re already at the portal),
            // force an escape move that exits the portal.
            if (path.Count < 2)
            {
                BotAction? escapeAction = null;
                // Determine the escape direction based on which edge the portal cell is on.
                if (portalTarget.X == 0 && PortalLeft)
                    escapeAction = BotAction.Left;
                else if (portalTarget.X == MapWidth - 1 && PortalRight)
                    escapeAction = BotAction.Right;
                else if (portalTarget.Y == 0 && PortalUp)
                    escapeAction = BotAction.Up;
                else if (portalTarget.Y == MapHeight - 1 && PortalDown)
                    escapeAction = BotAction.Down;

                if (escapeAction.HasValue)
                {
                    Console.WriteLine($"Tick: {state.Tick} | Escape move: {escapeAction.Value} (exiting portal)");
                    return new BotCommand { Action = escapeAction.Value };
                }
                else
                {
                    Console.WriteLine("No escape action computed.");
                    return null;
                }
            }

            // 6. Otherwise, use the normal computed path.
            var action = ComputeNextMoveRateLimited(myAnimal, path);
            if (!action.HasValue)
            {
                Console.WriteLine("No action computed from escape path.");
                return null;
            }

            // Remove the first node (current position) from the persistent path.
            PersistentPath.RemoveAt(0);

            Console.WriteLine($"Tick: {state.Tick} | Action: {action.Value} | EscapeZookeeper => move to portal");
            return new BotCommand { Action = action.Value };
        }
        public static HashSet<(int, int)> IdentifyNarrowAndLeadInPellets(Dictionary<(int, int), CellContent> grid)
        {
            var avoid = new HashSet<(int, int)>();
            foreach (var ((x, y), content) in grid)
            {
                if (content != CellContent.Pellet) continue;
                int wallCount = 0;
                var dirs = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

                foreach (var (dx, dy) in dirs)
                {
                    var neighbor = (x + dx, y + dy);
                    if (grid.TryGetValue(neighbor, out var nContent) && nContent == CellContent.Wall)
                        wallCount++;
                }

                if (wallCount >= 3)
                {
                    avoid.Add((x, y));
                    foreach (var (dx, dy) in dirs)
                    {
                        var neighbor = (x + dx, y + dy);
                        if (grid.TryGetValue(neighbor, out var nContent) && nContent == CellContent.Pellet)
                            avoid.Add(neighbor);
                    }
                }
            }
            return avoid;
        }

        public static HashSet<(int, int)> PredictContestedPellets(Animal myAnimal, GameState state)
        {
            if (state.Cells == null)
                return new HashSet<(int, int)>();

            var grid = state.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            var contested = new HashSet<(int, int)>();

            foreach (var animal in state.Animals)
            {
                if (animal.Id == myAnimal.Id) continue;

                var pellet = FindNearestPelletTieBreak(animal.X, animal.Y, state.Cells, null);
                if (pellet == null) continue;

                contested.Add((pellet.X, pellet.Y));


                //var path = LabRat.FindPathOpponent(animal.X, animal.Y, pellet.X, pellet.Y, grid, animal, state);
                //if (path == null) continue;

                //foreach (var step in path)
                //{
                //    contested.Add((step.X, step.Y));
                //}
            }

            return contested;
        }

    }

    public class LabRatNode
    {
        public int X;
        public int Y;
        public int G;
        public int H;
        public int F => G + H;
        public LabRatNode? Parent;
        public int TieBreak { get; }

        public LabRatNode(int x, int y, int g, int h, LabRatNode? parent, int tieBreak = 0)
        {
            X = x;
            Y = y;
            G = g;
            H = h;
            Parent = parent;
            TieBreak = tieBreak;
        }
    }
}