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
    public static class Mole
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
        public static List<MoleNode>? PersistentPath { get; set; } = null;
        public static double? PersistentPathScore = null;
        public static Dictionary<(int, int), int> VisitedCounts { get; set; } = new Dictionary<(int, int), int>();
        public static readonly Queue<(int, int)> RecentPositions = new Queue<(int, int)>();
        public static int stuckCounter = 0;
        public static readonly Random rng = new Random();
        public static BotAction? LastMove { get; set; } = null;
        public static Cell? CurrentTargetPellet = null;
        public static bool IsInDanger = false;
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
        public static (int X, int Y)? LastSentPosition = null;


        public static GameState GAME_STATE { get; set; } = new GameState();



        #endregion

        #region Game Stage Settings

        public static GameStage GAME_STAGE = GameStage.EarlyGame;

        // Tweakable parameters
        public static double ALPHA = 1.3;
        public static int VISITED_TILE_PENALTY_FACTOR = 2;
        public static int REVERSING_TO_PARENT_PENALTY = 5;
        public static double ZOOKEEPER_AVOIDANCE_FACTOR = 12;
        public static int ENEMY_PATH_AVOIDANCE = 4;
        public static int PELLET_BONUS = 9;
        public static int DANGER_THRESHOLD = 200;
        public static int MAX_PELLET_CANDIDATES = 5; // Number of pellets to consider for multi-pellet pathfinding
        public static int CORRIDOR_PENALTY = 25;

        //Early Game
        public static double EG_ALPHA = 1.9;
        public static int EG_VISITED_TILE_PENALTY_FACTOR = 2;
        public static int EG_REVERSING_TO_PARENT_PENALTY = 1;
        public static double EG_ZOOKEEPER_AVOIDANCE_FACTOR = 20;
        public static int EG_ENEMY_PATH_AVOIDANCE = 5;
        public static int EG_PELLET_BONUS = 12;
        public static int EG_DANGER_THRESHOLD = 1500;
        public static int EG_MAX_PELLET_CANDIDATES = 9;
        public static int EG_CORRIDOR_PENALTY = 1;

        //Late Game
        public static double LG_ALPHA = 1.3;
        public static int LG_VISITED_TILE_PENALTY_FACTOR = 2;
        public static int LG_REVERSING_TO_PARENT_PENALTY = 5;
        public static double LG_ZOOKEEPER_AVOIDANCE_FACTOR = 12;
        public static int LG_ENEMY_PATH_AVOIDANCE = 4;
        public static int LG_PELLET_BONUS = 9;
        public static int lG_DANGER_THRESHOLD = 200000;
        public static int LG_MAX_PELLET_CANDIDATES = 5;
        public static int LG_CORRIDOR_PENALTY = 25;

        #endregion

        #region Constants

        public const int SAFE_DISTANCE = 5;
        public const int STUCK_THRESHOLD = 3;
        public const int RECENT_POS_QUEUE_SIZE = 5;
        public const int REPLAN_INTERVAL = 3;
        public const bool ENABLE_LOGGING = false;

        #endregion


        //TODO
        



        #region Main methods
        public static BotCommand? ProcessState(GameState gameStateDTO, Guid id)
        {
            var stopwatch = Stopwatch.StartNew();
            LogMessage($"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx | START | xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

            GAME_STATE = gameStateDTO;

            try
            {
                if (GAME_STATE.Tick <= 2)
                {
                    ApplyParameters(GAME_STAGE);
                }

                if (GAME_STATE.Tick > 100 && GAME_STAGE != GameStage.MidGame)
                {
                    GAME_STAGE = GameStage.MidGame;
                    ApplyParameters(GAME_STAGE);
                }


                return CollectPellets(id);
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
                    LogMessage($"Time : {elapsed}", ConsoleColor.Red);
                    ExecutionTimeExceedCount++;
                }
                else
                {
                    LogMessage($"Time : {elapsed}", ConsoleColor.White);
                }

                if (elapsed < minExecutionTime) minExecutionTime = elapsed;
                if (elapsed > maxExecutionTime) maxExecutionTime = elapsed;

                LogMessage($"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx | END | xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

            }
        }
        public static void ApplyParameters(GameStage stage)
        {
            switch (stage)
            {
                case GameStage.EarlyGame:
                    Mole.ALPHA = EG_ALPHA;
                    Mole.VISITED_TILE_PENALTY_FACTOR = EG_VISITED_TILE_PENALTY_FACTOR;
                    Mole.REVERSING_TO_PARENT_PENALTY = EG_REVERSING_TO_PARENT_PENALTY;
                    Mole.ZOOKEEPER_AVOIDANCE_FACTOR = EG_ZOOKEEPER_AVOIDANCE_FACTOR;
                    Mole.ENEMY_PATH_AVOIDANCE = EG_ENEMY_PATH_AVOIDANCE;
                    Mole.PELLET_BONUS = EG_PELLET_BONUS;
                    Mole.DANGER_THRESHOLD = EG_DANGER_THRESHOLD;
                    Mole.MAX_PELLET_CANDIDATES = EG_MAX_PELLET_CANDIDATES;
                    Mole.CORRIDOR_PENALTY = EG_CORRIDOR_PENALTY;
                    break;
                case GameStage.MidGame:
                    Mole.ALPHA = LG_ALPHA;
                    Mole.VISITED_TILE_PENALTY_FACTOR = LG_VISITED_TILE_PENALTY_FACTOR;
                    Mole.REVERSING_TO_PARENT_PENALTY = LG_REVERSING_TO_PARENT_PENALTY;
                    Mole.ZOOKEEPER_AVOIDANCE_FACTOR = LG_ZOOKEEPER_AVOIDANCE_FACTOR;
                    Mole.ENEMY_PATH_AVOIDANCE = LG_ENEMY_PATH_AVOIDANCE;
                    Mole.PELLET_BONUS = LG_PELLET_BONUS;
                    Mole.DANGER_THRESHOLD = lG_DANGER_THRESHOLD;
                    Mole.MAX_PELLET_CANDIDATES = LG_MAX_PELLET_CANDIDATES;
                    Mole.CORRIDOR_PENALTY = LG_CORRIDOR_PENALTY;
                    break;
                case GameStage.LateGame:
                    break;
                default:
                    break;
            }
        }
        public static BotCommand? CollectPellets(Guid id)
        {
            // Update map dimensions
            MapWidth = GAME_STATE.Cells.Max(c => c.X) + 1;
            MapHeight = GAME_STATE.Cells.Max(c => c.Y) + 1;

            if (GAME_STATE.Tick == lastTickCommandIssued)
            {
                LogMessage($"Tick mismatch | Game State {GAME_STATE.Tick} | Last Tick Command {lastTickCommandIssued}", ConsoleColor.Red);
                return null;
            }

            var myAnimal = GAME_STATE.Animals.FirstOrDefault(x => x.Id == id);

            if (myAnimal == null)
            {
                LogMessage($"Cannot find my animal", ConsoleColor.Red);
                lastTickCommandIssued = GAME_STATE.Tick;
                return null;
            }

            LogMessage($"Current Location: ({myAnimal.X}, {myAnimal.Y})", ConsoleColor.White);

            // <<=== NEW: Validate engine sync if a command was issued last tick ===>>
            if (LastSentPosition.HasValue && LastMove.HasValue && GAME_STATE.Tick == lastTickCommandIssued + 1 && GAME_STATE.Tick > 5)
            {
                var expectedPos = GetExpectedPosition(LastSentPosition.Value, LastMove.Value);
                if (myAnimal.X != expectedPos.X || myAnimal.Y != expectedPos.Y)
                {
                    LogMessage($"Engine out-of-sync: Expected position ({expectedPos.X}, {expectedPos.Y}) but got ({myAnimal.X}, {myAnimal.Y}). Command skipped check queue.", ConsoleColor.Red);
                    lastTickCommandIssued = GAME_STATE.Tick;
                    LastSentPosition = null;
                    LastMove = null;

                    return null;
                }
            }


            // --- Respawn Safety Check ---
            //If the animal is at its spawn, check if any zookeeper is too close.
            if (((currentDeathCounter < myAnimal.CapturedCounter) || myAnimal.IsViable == false) && GAME_STATE.Tick > 5)
            {

                LogMessage("### RESPAWNING #####", ConsoleColor.Magenta);

                currentDeathCounter = myAnimal.CapturedCounter;
                if (!IsSafeToLeaveSpawn(myAnimal, SAFE_DISTANCE))
                {
                    LogMessage("Zookeeper too close to spawn. Waiting to leave spawn.", ConsoleColor.Magenta);
                    lastTickCommandIssued = GAME_STATE.Tick;
                    LogMessage($"Tick: {lastTickCommandIssued} | Waiting", ConsoleColor.Magenta);
                    return null;
                }
            }

            if (GAME_STAGE == GameStage.MidGame)
            {
                if (IsZookeeperNear(myAnimal))
                {
                    if (HasPortals())
                    {
                        PersistentPath = null;
                        PersistentTarget = null;
                        IGNORE_REPLAN = true;
                        IsInDanger = true;

                        LogMessage("##### DANGER DANGER DANGER DANGER DANGER #################################", ConsoleColor.Red);

                        var esacpeAction = EscapeZookeeper(myAnimal);

                        if (esacpeAction != null)
                        {
                            LastSentPosition = (myAnimal.X, myAnimal.Y); // Save starting position before the move is applied
                            LastMove = esacpeAction.Action;
                            lastTickCommandIssued = GAME_STATE.Tick;

                            return esacpeAction;   
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
                        var escapeAction = EscapeChasedZookeeper(myAnimal);
                        if (escapeAction != null)
                        {
                            LastSentPosition = (myAnimal.X, myAnimal.Y); // Save starting position before the move is applied
                            LastMove = escapeAction.Action;
                            lastTickCommandIssued = GAME_STATE.Tick;
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



            var portalDecision = EvaluateAndRunThroughPortal(myAnimal);
            if (portalDecision != null)
            {
                return portalDecision;
            }


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
                    LogMessage("Stuck threshold reached; clearing path & target.");
                    PersistentPath = null;
                    PersistentTarget = null;
                    stuckCounter = 0;

                    var tieBreakTarget = FindNearestPelletTieBreak(myAnimal.X, myAnimal.Y, GAME_STATE.Cells, LastMove);
                    if (tieBreakTarget != null)
                    {
                        LogMessage($"New target acquired (break out of loop fallback): ({tieBreakTarget.X}, {tieBreakTarget.Y})");
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

            if (!IsPathStillValid(PersistentPath, myAnimal, GAME_STATE.Cells))
            {
                LogMessage("Periodic check: path invalid => clearing path & target.");
                PersistentPath = null;
                PersistentTarget = null;

            }

            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);

            if ((GAME_STATE.Tick % REPLAN_INTERVAL == 0) && !IGNORE_REPLAN)
            {
                if (PersistentPath != null && PersistentPath.Count > 2)
                {
                    var currentScore = EvaluatePathScore(PersistentPath);
                    LogMessage($"[REPLAN CHECK] Current score: {currentScore:F2} | Original: {PersistentPathScore:F2}");


                    if (PersistentPathScore.HasValue && currentScore < PersistentPathScore.Value)
                    {
                        LogMessage("Replanning due to score drop.");
                        PersistentPath = null;
                        PersistentTarget = null;
                        PersistentPathScore = null;
                    }
                }
                else
                {
                    PersistentPath = null;
                    PersistentTarget = null;
                    PersistentPathScore = null;
                }
            }


            // Fallback to your current target selection logic:
            CurrentTargetPellet = ValidateOrFindTarget(myAnimal);
            PersistentTarget = CurrentTargetPellet;
            if (CurrentTargetPellet == null)
            {
                lastTickCommandIssued = GAME_STATE.Tick;
                return null;
            }



            // Compute or reuse path; note the additional parameters passed
            var path = ValidateOrComputePath(myAnimal, CurrentTargetPellet, grid);
            if (path == null || path.Count == 0)
            {
                LogMessage("No path found or path is empty. Skipping target.");
                PersistentTarget = null;
                PersistentPath = null;
                CurrentTargetPellet = null;
                lastTickCommandIssued = GAME_STATE.Tick;
                return null;
            }

            // Rate-limited move issuance
            BotAction? action = ComputeNextMoveRateLimited(myAnimal, path);
            if (!action.HasValue)
            {
                LogMessage("Rate-limited: Not issuing a new command this tick.");
                lastTickCommandIssued = GAME_STATE.Tick;
                return null;
            }


            PersistentPath?.RemoveAt(0);
            //PersistentPathScore =  EvaluatePathScore(PersistentPath, gameStateDTO);

            // If target reached, clear persistent state
            if (myAnimal.X == CurrentTargetPellet.X && myAnimal.Y == CurrentTargetPellet.Y)
            {
                LogMessage("Target reached. Clearing persistent data.");
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

            // <<=== UPDATE: Before issuing the command, store current position and last move ===>>
            LogMessage($"Tick: {GAME_STATE.Tick} | Moving {action.Value} | Score {myAnimal.Score}");
            LastSentPosition = (myAnimal.X, myAnimal.Y); // Save starting position before the move is applied
            LastMove = action.Value;
            lastTickCommandIssued = GAME_STATE.Tick;

            return new BotCommand { Action = action.Value };
            //}
        }

        #endregion

        #region Validation
        private static (int X, int Y) GetExpectedPosition((int X, int Y) start, BotAction move)
        {
            int newX = start.X;
            int newY = start.Y;
            switch (move)
            {
                case BotAction.Up:
                    newY--;
                    break;
                case BotAction.Down:
                    newY++;
                    break;
                case BotAction.Left:
                    newX--;
                    break;
                case BotAction.Right:
                    newX++;
                    break;
            }
            // Handle portal wrapping if enabled
            if (newX < 0 && PortalLeft)
                newX = MapWidth - 1;
            else if (newX >= MapWidth && PortalRight)
                newX = 0;
            if (newY < 0 && PortalUp)
                newY = MapHeight - 1;
            else if (newY >= MapHeight && PortalDown)
                newY = 0;
            return (newX, newY);
        }
        public static bool IsPathStillValid(List<MoleNode>? path, Animal myAnimal, List<Cell> allCells)
        {
            // Check that the path exists and that our animal is at the start of the path.
            if (path == null || path.Count == 0)
            {
                LogMessage("PATH INVALID: Path is null or empty.", ConsoleColor.Magenta);
                return false;
            }

            var firstNode = path[0];
            if ((myAnimal.X != firstNode.X || myAnimal.Y != firstNode.Y) && myAnimal.IsViable)
            {
                LogMessage($"PATH INVALID: Animal out of sync. Animal at ({myAnimal.X}, {myAnimal.Y}) | Path[0] at ({firstNode.X}, {firstNode.Y}) | Animal viable {myAnimal.IsViable}", ConsoleColor.Magenta);
                return false;
            }

            // Build a grid for cell lookups.
            var grid = allCells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            foreach (var node in path)
            {
                if (!grid.TryGetValue((node.X, node.Y), out var content))
                {
                    LogMessage($"PATH INVALID: Cannot get content at ({node.X}, {node.Y}): {content}", ConsoleColor.Magenta);
                    return false;
                }
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                {
                    LogMessage($"PATH INVALID: Invalid content at ({node.X}, {node.Y}): {content}", ConsoleColor.Magenta);
                    return false;
                }

            }

            // Compute the danger along the path.
            int totalDanger = 0;
            foreach (var node in path)
            {
                // Compute the zookeeper penalty at this node.
                // (Assuming ComputeZookeeperPenalty is defined elsewhere and uses state.Zookeepers)
                totalDanger += ComputeZookeeperPenalty(node.X, node.Y, myAnimal);

                if (totalDanger > DANGER_THRESHOLD)
                {
                    // Path is too dangerous.
                    LogMessage($"PATH INVALID: Path is too dangerous Total danger is {totalDanger} and danger threshold is {DANGER_THRESHOLD}");
                    return false;
                }

            }

            // Define a threshold for path danger (tune this value as needed).



            return true;
        }
        public static double EvaluatePathScore(List<MoleNode> path)
        {
            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            int pellets = CountPelletsOnPath(path, grid);
            return pellets;
        }

        #endregion

        #region Respawn logic
        public static bool IsSafeToLeaveSpawn(Animal myAnimal, int safeDistance)
        {
            foreach (var zk in GAME_STATE.Zookeepers)
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

        #endregion

        #region Zookeeper avoidance logic
        public static bool IsZookeeperNear(Animal myAnimal)
        {
            // Loop through each zookeeper in the game state.
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                int distance = Manhattan(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (distance <= (SAFE_DISTANCE * 2))
                {

                    var pathToMe = FindPathZookeeper(zk.X, zk.Y, myAnimal.X, myAnimal.Y, GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content));
                    if (pathToMe != null && pathToMe.Count <= SAFE_DISTANCE)
                    {
                        return true;
                    }
                }

            }
            return false;
        }
        public static bool HasPortals()
        {
            // Compute the map dimensions from the cells.
            int width = GAME_STATE.Cells.Max(c => c.X) + 1;
            int height = GAME_STATE.Cells.Max(c => c.Y) + 1;

            // A cell is considered passable if it is not a wall.
            // (You may need to adjust this depending on your game's rules.)
            bool leftEdgeOpen = GAME_STATE.Cells.Any(c => c.X == 0 && c.Content != CellContent.Wall);
            bool rightEdgeOpen = GAME_STATE.Cells.Any(c => c.X == width - 1 && c.Content != CellContent.Wall);
            bool topEdgeOpen = GAME_STATE.Cells.Any(c => c.Y == 0 && c.Content != CellContent.Wall);
            bool bottomEdgeOpen = GAME_STATE.Cells.Any(c => c.Y == height - 1 && c.Content != CellContent.Wall);

            // We consider portals available if either the horizontal or vertical edges are open.
            return (leftEdgeOpen && rightEdgeOpen) || (topEdgeOpen && bottomEdgeOpen);
        }
        public static IEnumerable<MoleNode> GetNeighborsZookeeper(
            MoleNode current,
            Dictionary<(int, int), CellContent> grid,
            int targetX,
            int targetY)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int nx = current.X + directions[i, 0];
                int ny = current.Y + directions[i, 1];



                if (!grid.TryGetValue((nx, ny), out var content))
                    continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn)
                    continue;




                int newG = current.G + 1;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new MoleNode(
                    nx, ny,
                    newG, newH,
                    current,
                    ComputeTieBreaker(nx, ny)
                );
            }
        }

        public static BotCommand? EscapeZookeeper(Animal myAnimal)
        {

            // 1. Find the closest zookeeper.
            if (GAME_STATE.Zookeepers == null || GAME_STATE.Zookeepers.Count == 0)
                return null;

            Zookeeper closestZookeeper = null;
            int minDistance = int.MaxValue;
            foreach (var zk in GAME_STATE.Zookeepers)
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


            var allPortalCellsOrdered = GAME_STATE.Cells
                    .Where(c => IsPortalCell(c) && c.Content != CellContent.Wall)
                    .OrderBy(c => Manhattan(myAnimal.X, myAnimal.Y, c.X, c.Y))
                    .ToList();

            if (Manhattan(myAnimal.X, myAnimal.Y, allPortalCellsOrdered.FirstOrDefault().X, allPortalCellsOrdered.FirstOrDefault().Y) < SAFE_DISTANCE && (Manhattan(myAnimal.X, myAnimal.Y, allPortalCellsOrdered.FirstOrDefault().X, allPortalCellsOrdered.FirstOrDefault().Y) > Manhattan(myAnimal.X, myAnimal.Y, closestZookeeper.X, closestZookeeper.Y)))
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
                LogMessage("No valid portal target found.", ConsoleColor.Green);
                return null;
            }

            // 4. Compute a path to the portal target.
            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            var path = ValidateOrComputePath(myAnimal, portalTarget, grid);
            if (path == null || path.Count == 0)
            {
                LogMessage("No path found to portal.", ConsoleColor.Green);
                return null;
            }

            int pellets_Left = GAME_STATE.Cells.Where(C => C.Content == CellContent.Pellet).Count();
            int total_pellets = GAME_STATE.Cells.Where(C => C.Content == CellContent.Pellet || C.Content == CellContent.Empty).Count();
            double pelletsLeftPercent = (double)pellets_Left / total_pellets * 100;

            // Find highest-scoring opponent
            var topOpponent = GAME_STATE.Animals.Where(x => x.Id != myAnimal.Id).OrderByDescending(o => o.Score).FirstOrDefault();
            double opponentScore = topOpponent?.Score ?? 0;

            if (pelletsLeftPercent >= 60)
            {
                // Estimate the value of escaping vs. dying
                int ticksToEscape = path.Count;
                double averagePelletsPerTick = 0.4; // Tweak this based on testing/data
                double estimatedPelletsLost = ticksToEscape * averagePelletsPerTick;
                double pelletLossIfCaught = myAnimal.Score * 0.10;

                LogMessage($"Escape decision -> Pellets left {pelletsLeftPercent:F1}%, TicksToEscape: {ticksToEscape}, EstPelletsLost: {estimatedPelletsLost:F1}, OnDeathLoss: {pelletLossIfCaught:F1}", ConsoleColor.Green);

                if (pelletLossIfCaught < estimatedPelletsLost)
                {
                    LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);
                    return null; // Don't escape, let the normal logic handle it
                }
            }
            else if (pelletsLeftPercent >= 10)
            {
                double hit_loss = myAnimal.Score * 0.10;


                if (myAnimal.Score < opponentScore)
                {
                    double score_left = pellets_Left / 11;

                    LogMessage($"Escape decision -> Pellets left {pelletsLeftPercent:F1}%, Hit Loss: {hit_loss:F1}, Score Left: {score_left:F1}", ConsoleColor.Green);

                    if (score_left > hit_loss)
                    {
                        LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);
                        return null; // Don't escape, let the normal logic handle it
                    }
                }
                else
                {
                    if ((myAnimal.Score - hit_loss) < opponentScore)
                    {
                        //will loose the lead
                        LogMessage($"Escape decision -> Keep the lead");
                    }
                    else
                    {
                        double score_left = pellets_Left / 4;

                        LogMessage($"Escape decision -> Pellets left {pelletsLeftPercent:F1}%, Hit Loss: {hit_loss:F1}, Score Left: {score_left:F1}", ConsoleColor.Green);

                        if (score_left > hit_loss)
                        {
                            LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);

                        }
                    }
                }


            }
            else
            {
                LogMessage($"Escape decision -> Pellets left {pelletsLeftPercent:F1}%");
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
                    LogMessage($"Tick: {GAME_STATE.Tick} | Escape move: {escapeAction.Value} (exiting portal)", ConsoleColor.Green);
                    return new BotCommand { Action = escapeAction.Value };
                }
                else
                {
                    LogMessage("No escape action computed.", ConsoleColor.Green);
                    return null;
                }
            }

            // 6. Otherwise, use the normal computed path.
            var action = ComputeNextMoveRateLimited(myAnimal, path);
            if (!action.HasValue)
            {
                LogMessage("No action computed from escape path.", ConsoleColor.Green);
                return null;
            }

            // Remove the first node (current position) from the persistent path.
            PersistentPath.RemoveAt(0);

            LogMessage($"Tick: {GAME_STATE.Tick} | Action: {action.Value} | EscapeZookeeper => move to portal", ConsoleColor.Green);
            return new BotCommand { Action = action.Value };
        }

        public static BotCommand? EscapeChasedZookeeper(Animal myAnimal)
        {
            // 1. Identify the closest zookeeper.
            Zookeeper? closestZookeeper = null;
            int minDistance = int.MaxValue;
            foreach (var zk in GAME_STATE.Zookeepers)
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


            LogMessage($"Escape decision -> TicksToEscape: {ticksToEscape}, EstPelletsLost: {estimatedPelletsLost:F1}, OnDeathLoss: {pelletLossIfCaught:F1}", ConsoleColor.Green);

            if (pelletLossIfCaught < estimatedPelletsLost)
            {
                LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);
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
                var cell = GAME_STATE.Cells.FirstOrDefault(c => c.X == newX && c.Y == newY);
                if (cell != null && cell.Content != CellContent.Wall &&
                    cell.Content != CellContent.AnimalSpawn && cell.Content != CellContent.ZookeeperSpawn)
                {
                    LogMessage($"Chased escape: moving {primaryAction}", ConsoleColor.Green);
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
                var altCell = GAME_STATE.Cells.FirstOrDefault(c => c.X == altX && c.Y == altY);
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
                LogMessage($"Chased escape alternative: moving {bestAction}", ConsoleColor.Green);
                return new BotCommand { Action = bestAction.Value };
            }
            return null;
        }

        #endregion

        #region Portal logic
        public static BotCommand? EvaluateAndRunThroughPortal(Animal myAnimal)
        {
            if (!IsNearPortalCell(myAnimal))
                return null;


            LogMessage($"[EVALUATE PORTAL]", ConsoleColor.Blue);

            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);

            // Determine portal side
            bool isLeft = myAnimal.X <= 5;
            bool isRight = myAnimal.X >= MapWidth - 6;
            bool isTop = myAnimal.Y <= 5;
            bool isBottom = myAnimal.Y >= MapHeight - 6;

            Func<Cell, bool> isThisSide = c => true;
            Func<Cell, bool> isOtherSide = c => false;

            if (isLeft || isRight)
            {
                isThisSide = c => (isLeft && c.X < MapWidth / 2) || (isRight && c.X >= MapWidth / 2);
                isOtherSide = c => !isThisSide(c);
            }
            else if (isTop || isBottom)
            {
                isThisSide = c => (isTop && c.Y < MapHeight / 2) || (isBottom && c.Y >= MapHeight / 2);
                isOtherSide = c => !isThisSide(c);
            }

            // Evaluate pellets
            int thisPellets = GAME_STATE.Cells.Count(c => c.Content == CellContent.Pellet && isThisSide(c));
            int otherPellets = GAME_STATE.Cells.Count(c => c.Content == CellContent.Pellet && isOtherSide(c));

            // Evaluate enemies
            int thisEnemies = GAME_STATE.Animals.Count(a => a.Id != myAnimal.Id && a.IsViable && isThisSide(new Cell { X = a.X, Y = a.Y }));
            int otherEnemies = GAME_STATE.Animals.Count(a => a.Id != myAnimal.Id && a.IsViable && isOtherSide(new Cell { X = a.X, Y = a.Y }));

            //// Evaluate zookeepers
            //int thisZKs = state.Zookeepers.Count(z => isThisSide(new Cell { X = z.X, Y = z.Y }));
            var otherZKs = GAME_STATE.Zookeepers.Where(z => isOtherSide(new Cell { X = z.X, Y = z.Y }));

            // Predict where we'll exit
            int exitX = myAnimal.X;
            int exitY = myAnimal.Y;

            if (isLeft && PortalRight) exitX = MapWidth - 1;
            else if (isRight && PortalLeft) exitX = 0;
            else if (isTop && PortalDown) exitY = MapHeight - 1;
            else if (isBottom && PortalUp) exitY = 0;

            // Safety check for zookeepers at portal exit
            bool portalExitIsSafe = true;
            foreach (var zk in otherZKs)
            {
                int dist = Manhattan(exitX, exitY, zk.X, zk.Y);
                if (dist < SAFE_DISTANCE + 1)
                {
                    portalExitIsSafe = false;
                    LogMessage($"[[EVALUATE PORTAL]] Unsafe portal exit! Zookeeper at ({zk.X}, {zk.Y}) is only {dist} away from predicted exit ({exitX}, {exitY})", ConsoleColor.Red);
                    break;
                }
            }


            // Decision logic
            if (otherPellets > thisPellets && otherEnemies <= thisEnemies && portalExitIsSafe)
            {
                // Find closest valid portal cell on the edge
                var portalCandidates = GAME_STATE.Cells
                    .Where(c => IsPortalCell(c) && c.Content != CellContent.Wall)
                    .OrderBy(c => Manhattan(myAnimal.X, myAnimal.Y, c.X, c.Y))
                    .ToList();

                var targetPortal = portalCandidates.FirstOrDefault();
                if (targetPortal == null)
                    return null;

                var path = FindPath(myAnimal.X, myAnimal.Y, targetPortal.X, targetPortal.Y, grid, myAnimal);
                if (path != null && path.Count > 1)
                {
                    LogMessage($"[EVALUATE PORTAL] Chose to cross map to better zone via portal at ({targetPortal.X},{targetPortal.Y}) [P:{thisPellets} A:{thisEnemies} S:{portalExitIsSafe}] / [P:{otherPellets} A:{otherEnemies} S:{portalExitIsSafe}]", ConsoleColor.DarkBlue);

                    PersistentPath = path;
                    PersistentTarget = targetPortal;

                    var action = ComputeNextMoveRateLimited(myAnimal, path);
                    if (action.HasValue)
                    {
                        LastSentPosition = (myAnimal.X, myAnimal.Y);
                        LastMove = action.Value;
                        lastTickCommandIssued = GAME_STATE.Tick;
                        path.RemoveAt(0);
                        return new BotCommand { Action = action.Value };
                    }
                }
                else if (path.Count < 2)
                {

                    LogMessage($"[EVALUATE PORTAL] Chose to cross map to better zone via portal at ({targetPortal.X},{targetPortal.Y}) [P:{thisPellets} A:{thisEnemies} S:{portalExitIsSafe}] / [P:{otherPellets} A:{otherEnemies} S:{portalExitIsSafe}]", ConsoleColor.DarkBlue);



                    BotAction? escapeAction = null;
                    // Determine the escape direction based on which edge the portal cell is on.
                    if (targetPortal.X == 0 && PortalLeft)
                        escapeAction = BotAction.Left;
                    else if (targetPortal.X == MapWidth - 1 && PortalRight)
                        escapeAction = BotAction.Right;
                    else if (targetPortal.Y == 0 && PortalUp)
                        escapeAction = BotAction.Up;
                    else if (targetPortal.Y == MapHeight - 1 && PortalDown)
                        escapeAction = BotAction.Down;

                    if (escapeAction.HasValue)
                    {
                        //Console.WriteLine($"Tick: {state.Tick} | Escape move: {escapeAction.Value} (exiting portal)");
                        return new BotCommand { Action = escapeAction.Value };
                    }

                }


            }
            else
            {
                LogMessage($"[EVALUATE PORTAL] Chose to stay this side [P:{thisPellets} A:{thisEnemies} S:{portalExitIsSafe}] / [P:{otherPellets} A:{otherEnemies} S:{portalExitIsSafe}]", ConsoleColor.DarkBlue);
            }

            return null;
        }
        public static bool IsNearPortalCell(Animal myAnimal, int distance = 1)
        {
            var portals = GetAllPortals();

            if (portals.Any())
            {
                foreach (var portal in portals)
                {
                    var dist = Manhattan(myAnimal.X, myAnimal.Y, portal.X, portal.Y);
                    if (dist <= distance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public static List<Cell> GetAllPortals()
        {
            int width = GAME_STATE.Cells.Max(c => c.X) + 1;
            int height = GAME_STATE.Cells.Max(c => c.Y) + 1;
            return GAME_STATE.Cells.Where(c => (c.X == 0 || c.X == width - 1 || c.Y == 0 || c.Y == height - 1) && c.Content != CellContent.Wall).ToList();
        }
        public static bool IsPortalCell(Cell cell)
        {
            // A cell is on a portal if it is on any edge of the map.
            return (cell.X == 0 || cell.X == MapWidth - 1 || cell.Y == 0 || cell.Y == MapHeight - 1);
        }

        #endregion

        #region Target finding logic
        public static Cell? ValidateOrFindTarget(Animal myAnimal)
        {
            if (PersistentTarget != null)
            {
                bool stillPellet = GAME_STATE.Cells.Any(c =>
                    c.X == PersistentTarget.X &&
                    c.Y == PersistentTarget.Y &&
                    c.Content == CellContent.Pellet);
                if (stillPellet)
                {
                    LogMessage($"Using persistent target: ({PersistentTarget.X}, {PersistentTarget.Y})");    
                    return PersistentTarget;
                }
            }
            // Use the combined cluster + multi-pellet method to get a candidate.
            if (GAME_STAGE == GameStage.EarlyGame)
            {
                var (candidatePellet, candidatePath) = FindBestClusterMultiPelletPath(myAnimal);
                if (candidatePellet != null && candidatePath != null)
                {
                    LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y}) with a path of length {candidatePath.Count}");
                    PersistentTarget = candidatePellet;
                    PersistentPath = candidatePath;
                    PersistentPathScore = EvaluatePathScore(candidatePath);
                    return candidatePellet;
                }
            }

            if (GAME_STAGE == GameStage.MidGame)
            {
                var singlePellet = SinglePelletPlan(myAnimal, GAME_STATE.Cells, MAX_PELLET_CANDIDATES);

                if (singlePellet != null)
                {
                    LogMessage($"[SinglePellet] Selected target: ({singlePellet.X}, {singlePellet.Y})");
                    PersistentTarget = singlePellet;
                    PersistentPath = null;
                    return singlePellet;
                }
                else
                {
                    var (candidatePellet, candidatePath) = FindBestClusterMultiPelletPath(myAnimal);
                    if (candidatePellet != null && candidatePath != null)
                    {
                        LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y}) with a path of length {candidatePath.Count}");
                        PersistentTarget = candidatePellet;
                        PersistentPath = candidatePath;
                        PersistentPathScore = EvaluatePathScore(candidatePath);
                        return candidatePellet;
                    }
                }
            }


            var tieBreakTarget = FindNearestPelletTieBreak(myAnimal.X, myAnimal.Y, GAME_STATE.Cells, LastMove);
            if (tieBreakTarget != null)
            {
                LogMessage($"New target acquired (tie-break fallback): ({tieBreakTarget.X}, {tieBreakTarget.Y})");
                PersistentTarget = tieBreakTarget;
                PersistentPath = null;
            }
            return tieBreakTarget;
        }
        public static (Cell? bestPellet, List<MoleNode>? bestPath) FindBestClusterMultiPelletPath(Animal myAnimal)
        {
            // Build a grid for cell lookup.
            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);

            // 1. Identify a candidate pellet from the largest cluster.
            //    (This uses your existing FindPelletInLargestConnectedClusterWeighted.)
            var clusterCandidate = FindPelletInLargestConnectedClusterWeighted(myAnimal, GAME_STATE.Cells, ALPHA);
            if (clusterCandidate == null)
            {
                // If no cluster candidate is found, fall back to the multi-pellet method over all pellets.
                return FindBestMultiPelletPath(myAnimal, grid);
            }

            // 2. Obtain the full cluster containing the candidate pellet.
            var visited = new HashSet<(int, int)>();
            List<Cell> bestCluster = BFSCluster((clusterCandidate.X, clusterCandidate.Y), grid, visited);

            // 3. Filter candidate pellets to those in the identified cluster.
            var clusterPellets = bestCluster
                .OrderBy(p => Manhattan(myAnimal.X, myAnimal.Y, p.X, p.Y))
                .Take(MAX_PELLET_CANDIDATES)
                .ToList();



            // 4. For each pellet in the cluster, compute a path and score it.
            //if (GAME_STAGE == GameStage.EarlyGame)
            //{
            Cell? bestPellet = null;
            List<MoleNode>? bestPath = null;
            double bestScore = double.NegativeInfinity;

            foreach (var pellet in clusterPellets)
            {
                // Run your existing A* search (FindPath) to get a path from your current location to the pellet.
                var path = FindPath(myAnimal.X, myAnimal.Y, pellet.X, pellet.Y, grid, myAnimal);
                if (path == null || path.Count == 0)
                    continue;

                // Count how many pellets the path passes through.
                int pelletCountOnPath = CountPelletsOnPath(path, grid);

                int pathLength = path.Count;
                // Compute the score: reward higher pellet count and penalize longer paths.
                double score = pelletCountOnPath - (ALPHA * pathLength);

                // 5. If this candidate's score is the best so far, keep it.
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPellet = pellet;
                    bestPath = path;
                }
            }

            return (bestPellet, bestPath);
            //}
            //else
            //{
            //    double bestScore = double.NegativeInfinity;
            //    Cell? bestRoot = null;

            //    foreach (var pellet in clusterPellets)
            //    {
            //        double score = ComputeClusterMSTScore(pellet, clusterPellets, state);
            //        if (score > bestScore)
            //        {
            //            bestScore = score;
            //            bestRoot = pellet;
            //        }
            //    }
            //    var path = FindPath(myAnimal.X, myAnimal.Y, bestRoot.X, bestRoot.Y, grid, myAnimal, state);

            //    return (bestRoot, path);

            //}
        }
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
        public static (Cell? bestPellet, List<MoleNode>? bestPath) FindBestMultiPelletPath(
            Animal myAnimal,
            Dictionary<(int, int), CellContent> grid)
        {
            // 1. Gather all pellets and sort by nearest to the animal
            var allPellets = GAME_STATE.Cells
                .Where(c => c.Content == CellContent.Pellet)
                .OrderBy(c => Manhattan(myAnimal.X, myAnimal.Y, c.X, c.Y))
                .ToList();
            if (allPellets.Count == 0)
            {
                return (null, null);
            }

            // 2. Limit to the nearest N pellets to avoid huge overhead
            var candidatePellets = allPellets.Take(MAX_PELLET_CANDIDATES).ToList();

            Cell? bestPellet = null;
            List<MoleNode>? bestPath = null;
            double bestScore = double.NegativeInfinity;

            // 3. For each candidate pellet, run FindPath
            foreach (var pellet in candidatePellets)
            {
                var path = FindPath(
                    myAnimal.X,
                    myAnimal.Y,
                    pellet.X,
                    pellet.Y,
                    grid,
                    myAnimal
                );

                if (path == null || path.Count == 0)
                    continue;

                // 4. Count how many pellets are on this path
                int pelletCountOnPath = CountPelletsOnPath(path, grid);

                // 5. Compute the path score
                int pathLength = path.Count;
                double score = pelletCountOnPath - (ALPHA * pathLength);

                // 6. Keep track of the best
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPellet = pellet;
                    bestPath = path;
                }
            }

            return (bestPellet, bestPath);
        }

        public static Cell? SinglePelletPlan(
            Animal myAnimal,
            List<Cell> allCells,
            int maxPelletsToChain)
        {
            // Gather & sort all pellets by distance
            var pelletCells = allCells
                .Where(c =>
                    c.Content == CellContent.Pellet &&
                    Manhattan(myAnimal.X, myAnimal.Y, c.X, c.Y) == 1 &&
                    (
                        (c.X == myAnimal.X && Math.Abs(c.Y - myAnimal.Y) == 1) ||
                        (c.Y == myAnimal.Y && Math.Abs(c.X - myAnimal.X) == 1)
                    )
                    && LastMove.HasValue && IsAligned(LastMove, myAnimal.X, myAnimal.Y, c)
                )
                //.OrderByDescending(c => LastMove.HasValue && IsAligned(LastMove, myAnimal.X, myAnimal.Y, c))
                .ToList();

            if (pelletCells.Count == 0)
            {
                return null;
            }

            // Optionally limit how many total pellets we consider
            return pelletCells.FirstOrDefault();
            // the * 2 is just an example so we have a few extra to combine


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

        #endregion

        #region Path finding logic 
        public static List<MoleNode>? ValidateOrComputePath(Animal myAnimal, Cell target, Dictionary<(int, int), CellContent> grid)
        {
            if (PersistentPath != null && PersistentPath.Count > 0)
            {
                LogMessage("Using persistent path.");
                PersistentPathScore = EvaluatePathScore(PersistentPath);

                LogCurrentPath(PersistentPath);
                return PersistentPath;
            }
            var path = FindPath(myAnimal.X, myAnimal.Y, target.X, target.Y, grid, myAnimal);
            PersistentPath = path;
            PersistentPathScore = EvaluatePathScore(path);

            if (path != null && path.Count > 0)
            {
                LogMessage("Computed new path.");
                LogCurrentPath(PersistentPath);
            }
            return path;
        }
        public static List<MoleNode>? FindPath(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid,
            Animal myAnimal)
        {


            if (GAME_STAGE == GameStage.MidGame)
            {
                NarrowPelletAvoidanceSet = IdentifyNarrowAndLeadInPellets(grid);
                ContestedPelletsThisTick = PredictContestedPellets(myAnimal);
            }


            var openSet = new List<MoleNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new MoleNode(
                startX, startY,
                0,
                Manhattan(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY)
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

                foreach (var neighbor in GetNeighborsPortal(current, grid, targetX, targetY, myAnimal))
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
            
        public static IEnumerable<MoleNode> GetNeighborsPortal(
            MoleNode current,
            Dictionary<(int, int), CellContent> grid,
            int targetX,
            int targetY,
            Animal myAnimal)
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
                        int contestedPenalty = ComputeContestedPenatly(myAnimal, nx, ny);
                        additionalPenalty += contestedPenalty;
                    }

                }


                int pelletBonus = (content == CellContent.Pellet) ? PELLET_BONUS : 0;
                int zookeeperPenalty = ComputeZookeeperPenalty(nx, ny, myAnimal);



                int newG = current.G + 1 + additionalPenalty + zookeeperPenalty - pelletBonus;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new MoleNode(
                    nx, ny,
                    newG, newH,
                    current,
                    ComputeTieBreaker(nx, ny)
                );
            }
        }

        public static int ComputeContestedPenatly(Animal myAnimal, int nx, int ny)
        {
            bool otherAnimalColser = false;
            int additonPenalty = 0;
            if (ContestedPelletsThisTick.Contains((nx, ny)))
            {
                int myDist = Manhattan(myAnimal.X, myAnimal.Y, nx, ny);
                foreach (var opp in GAME_STATE.Animals)
                {
                    if (opp.Id == myAnimal.Id || opp.IsViable == false)
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
        public static int ComputeZookeeperPenalty(int nx, int ny, Animal myAnimal)
        {
            int penalty = 0;
            if (GAME_STATE.Zookeepers != null)
            {
                foreach (var zk in GAME_STATE.Zookeepers)
                {
                    int dist = Manhattan(nx, ny, zk.X, zk.Y);
                    // Avoid division by zero and scale penalty relative to risk (current score * 0.1)
                    penalty += (int)(ZOOKEEPER_AVOIDANCE_FACTOR * (myAnimal.Score * 0.1) / Math.Max(dist, 1));
                }
            }
            return penalty;
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

        public static HashSet<(int, int)> PredictContestedPellets(Animal myAnimal)
        {
            if (GAME_STATE.Cells == null)
                return new HashSet<(int, int)>();

            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            var contested = new HashSet<(int, int)>();

            foreach (var animal in GAME_STATE.Animals)
            {
                if (animal.Id == myAnimal.Id) continue;

                var pellet = FindNearestPelletTieBreak(animal.X, animal.Y, GAME_STATE.Cells, null);
                if (pellet == null) continue;

                contested.Add((pellet.X, pellet.Y));


                var path = Mole.FindPathOpponent(animal.X, animal.Y, pellet.X, pellet.Y, grid, animal);
                if (path != null && path.Count > 1)
                {
                    for (int i = 1; i < Math.Min(3, path.Count); i++)
                    {
                        var step = path[i];
                        contested.Add((step.X, step.Y));
                    }


                    //// The second node in the path is the cell the opponent is likely to move into next.
                    //var nextStep = path[1];
                    //contested.Add((nextStep.X, nextStep.Y));

                    //if (path != null && path.Count > 2)
                    //{
                    //    // The second node in the path is the cell the opponent is likely to move into next.
                    //    var nextStep2 = path[2];
                    //    contested.Add((nextStep2.X, nextStep2.Y));

                    //    if (path != null && path.Count > 3)
                    //    {
                    //        // The second node in the path is the cell the opponent is likely to move into next.
                    //        var nextStep3 = path[3];
                    //        contested.Add((nextStep3.X, nextStep3.Y));
                    //    }

                    //}


                }
            }

            //REMOVE THIS IF MULTIPLE ZOOKEEPERS
            foreach (var zookeeper in GAME_STATE.Zookeepers)
            {

                var animal = FindNearestAnimalToZookeeper(zookeeper.X, zookeeper.Y, GAME_STATE.Animals, null);
                if (animal == null) continue;

                var path = Mole.FindPathZookeeper(zookeeper.X, zookeeper.Y, animal.X, animal.Y, grid);
                if (path != null && path.Count > 1)
                {
                    for (int i = 1; i < Math.Min(3, path.Count); i++)
                    {
                        var step = path[i];
                        contested.Add((step.X, step.Y));
                    }
                }
            }

            return contested;
        }
        public static List<MoleNode>? FindPathOpponent(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid,
            Animal someAnimal)
        {

            var openSet = new List<MoleNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new MoleNode(
                startX, startY,
                0,
                Manhattan(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY)
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

                foreach (var neighbor in GetNeighborsPortalOpponent(current, grid, targetX, targetY, someAnimal))
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
        public static IEnumerable<MoleNode> GetNeighborsPortalOpponent(
                MoleNode current,
                Dictionary<(int, int), CellContent> grid,
                int targetX,
                int targetY,
                Animal someAnimal)
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
                int zookeeperPenalty = ComputeZookeeperPenalty(nx, ny, someAnimal);

                int newG = current.G + 1 + additionalPenalty + zookeeperPenalty - pelletBonus;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new MoleNode(
                    nx, ny,
                    newG, newH,
                    current,
                    ComputeTieBreaker(nx, ny)
                );
            }
        }

        public static Cell? FindNearestAnimalToZookeeper(int zookeeperX, int zookeeperY, List<Animal> animals, BotAction? lastMove)
        {
            int bestDistance = int.MaxValue;
            var animalCells = new List<Cell>();
            foreach (var a in animals)
            {
                int dist = Math.Abs(zookeeperX - a.X) + Math.Abs(zookeeperX - a.Y);
                if (dist < bestDistance)
                    bestDistance = dist;
                animalCells.Add(new Cell() { X = a.X, Y = a.Y });
            }
            var closest = animals.Where(p => Math.Abs(zookeeperX - p.X) + Math.Abs(zookeeperX - p.Y) == bestDistance).ToList();
            if (closest.Count == 0) return null;
            return new Cell() { X = closest[0].X, Y = closest[0].Y };
        }

        public static List<MoleNode>? FindPathZookeeper(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid)
        {

            var openSet = new List<MoleNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new MoleNode(
                startX, startY,
                0,
                Manhattan(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY)
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

                foreach (var neighbor in GetNeighborsZookeeper(current, grid, targetX, targetY))
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





        #endregion

        #region Command finding logic
        public static BotAction? ComputeNextMoveRateLimited(Animal myAnimal, List<MoleNode> path)
        {
            if (path.Count == 0)
            {
                LogMessage("Rate Limited: No path available for next move.", ConsoleColor.Blue);

                return null;
            }
            var firstStep = path[0];
            if ((myAnimal.X != firstStep.X || myAnimal.Y != firstStep.Y) && myAnimal.IsViable)
            {
                LogMessage($"Rate Limited: Out of sync | Animal is at ({myAnimal.X}, {myAnimal.Y}) | Path[0] is at ({firstStep.X},{firstStep.Y}) | Is Viable : {myAnimal.IsViable} ", ConsoleColor.Blue);
                return null;
            }

            if (path.Count < 2)
            {
                LogMessage($"Rate Limited: Path is too short | Animal is at ({myAnimal.X}, {myAnimal.Y}) | Path[0] is at ({firstStep.X},{firstStep.Y}) | Is Viable : {myAnimal.IsViable} | Last Move: {LastMove}", ConsoleColor.Blue);
                return null;
            }
            var secondStep = path[1];
            var action = GetDirection(firstStep.X, firstStep.Y, secondStep.X, secondStep.Y);

            return action;
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

        #endregion

        #region Utility
        public static int Manhattan(int x, int y, int tx, int ty)
        {
            return Math.Abs(x - tx) + Math.Abs(y - ty);
        }
        public static int ComputeTieBreaker(int x, int y)
        {
            if (GAME_STAGE == GameStage.EarlyGame)
            {
                return 0;
            }

            // Compute Manhattan distance to the nearest zookeeper; higher distance means safer.
            if (GAME_STATE.Zookeepers == null || GAME_STATE.Zookeepers.Count == 0)
                return 0;
            int minDistance = int.MaxValue;
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                int d = Manhattan(x, y, zk.X, zk.Y);
                if (d < minDistance)
                    minDistance = d;
            }
            // Use the negative so that a larger distance (safer) yields a lower tie-breaker value.
            return -minDistance;
        }
        public static List<MoleNode> ReconstructPath(MoleNode node)
        {
            var path = new List<MoleNode>();
            while (node != null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }
        private static int CountPelletsOnPath(List<MoleNode> path, Dictionary<(int, int), CellContent> grid)
        {
            int count = 0;
            foreach (var node in path)
            {
                if (grid.TryGetValue((node.X, node.Y), out var content))
                {
                    if (content == CellContent.Pellet)
                        count++;
                }
            }
            return count;
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

        public static int FindDistance(int x, int y, int tx, int ty)
        {
            var something = FindPathZookeeper(x, y, tx, ty, GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content));
            return something != null ? something.Count : 0;
        }

        #endregion

        #region Logging

        public static void LogMessage(string message, ConsoleColor color = ConsoleColor.White)
        {
            if (ENABLE_LOGGING)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        public static void LogCurrentPath(List<MoleNode> path)
        {
            var coords = path.Select(n => $"({n.X},{n.Y})");
            LogMessage($"Current path (Score: {PersistentPathScore}): " + string.Join(" -> ", coords));
        }

        #endregion


    }

    public class MoleNode
    {
        public int X;
        public int Y;
        public int G;
        public int H;
        public int F => G + H;
        public MoleNode? Parent;
        public int TieBreak { get; }

        public MoleNode(int x, int y, int g, int h, MoleNode? parent, int tieBreak = 0)
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