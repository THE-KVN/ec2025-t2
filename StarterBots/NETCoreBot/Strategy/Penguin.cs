using NETCoreBot.Enums;
using NETCoreBot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NETCoreBot.Strategy
{
    public static class Penguin
    {
        // --- Portal/Map Configuration ---
        public static int MapWidth = 30;  // Replaced at runtime
        public static int MapHeight = 20; // Replaced at runtime

        public static bool PortalLeft = true;
        public static bool PortalRight = true;
        public static bool PortalUp = true;
        public static bool PortalDown = true;

        public static int lastTickCommandIssued = 0;
        public static Cell? PersistentTarget { get; set; } = null;
        public static List<PenguinNode>? PersistentPath { get; set; } = null;

        public static Dictionary<(int, int), int> VisitedCounts { get; set; } = new Dictionary<(int, int), int>();

        public static readonly Queue<(int, int)> RecentPositions = new Queue<(int, int)>();
        public static int stuckCounter = 0;
        public static readonly Random rng = new Random();
        public static BotAction? LastMove { get; set; } = null;
        public static Cell? CurrentTargetPellet = null;

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

        // Tweakable parameters
        public static double ALPHA = 0.7;
        public static int VISITED_TILE_PENALTY_FACTOR = 2;
        public static double REVERSAL_RANDOM_SKIP_CHANCE = 0.3;
        public static int REVERSING_TO_PARENT_PENALTY = 4;
        public static double ZOOKEEPER_AVOIDANCE_FACTOR = 1.0;
        public static int DANGER_THRESHOLD = 50;
        public static int SAFE_DISTANCE = 5;
        public static int REPLAN_INTERVAL = 5;
        public static int CORRIDOR_PENALTY = 10;


        public static bool IGNORE_REPLAN = false;
        public static HashSet<(int, int)> NarrowPelletAvoidanceSet = new();

        //constants
        public const int PELLET_BONUS = 1;
        public const int STUCK_THRESHOLD = 1;
        public const int RECENT_POS_QUEUE_SIZE = 4;

        public static BotCommand? ProcessState(GameState gameStateDTO, Guid id)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {





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
                    ExecutionTimeExceedCount++;
                }
                if (elapsed < minExecutionTime) minExecutionTime = elapsed;
                if (elapsed > maxExecutionTime) maxExecutionTime = elapsed;
            }
        }

        public static BotCommand? CollectPellets(GameState gameStateDTO, Guid id)
        {
            if (gameStateDTO.Tick == lastTickCommandIssued)
                return null;

            var myAnimal = gameStateDTO.Animals.FirstOrDefault(x => x.Id == id);
            if (myAnimal == null)
            {
                lastTickCommandIssued = gameStateDTO.Tick;
                return null;
            }

            // Update map dimensions
            MapWidth = gameStateDTO.Cells.Max(c => c.X) + 1;
            MapHeight = gameStateDTO.Cells.Max(c => c.Y) + 1;

            // --- Respawn Safety Check ---
            // If the animal is at its spawn, check if any zookeeper is too close.
            //if (myAnimal.X == myAnimal.SpawnX && myAnimal.Y == myAnimal.SpawnY)
            if ((currentDeathCounter < myAnimal.CapturedCounter) || (myAnimal.X == myAnimal.SpawnX && myAnimal.Y == myAnimal.SpawnY))
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

            if (HasPortals(gameStateDTO))
            {
                if (IsZookeeperNear(myAnimal, gameStateDTO))
                {
                    PersistentPath = null;
                    PersistentTarget = null;
                    IGNORE_REPLAN = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("##### DANGER DANGER DANGER DANGER DANGER #################################");
                    Console.ForegroundColor = ConsoleColor.White;

                    var esacpeAction = EscapeZookeeper(gameStateDTO, myAnimal);

                    if (esacpeAction != null)
                    {
                        return esacpeAction;
                    }
                }
            }

            IGNORE_REPLAN = false;
            Console.ForegroundColor = ConsoleColor.White;

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
            if ((gameStateDTO.Tick % REPLAN_INTERVAL == 0) && !IGNORE_REPLAN)
            {
                if (!IsPathStillValid(PersistentPath, myAnimal, gameStateDTO.Cells, gameStateDTO))
                {
                    Console.WriteLine("Periodic check: path invalid => clearing path & target.");
                    PersistentPath = null;
                    PersistentTarget = null;
                }
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

            Console.WriteLine($"Tick: {gameStateDTO.Tick} | Moving {action.Value}");
            LastMove = action.Value;
            PersistentPath?.RemoveAt(0);

            // If target reached, clear persistent state
            if (myAnimal.X == target.X && myAnimal.Y == target.Y)
            {
                Console.WriteLine("Target reached. Clearing persistent data.");
                PersistentTarget = null;
                PersistentPath = null;
            }

            lastTickCommandIssued = gameStateDTO.Tick;
            return new BotCommand { Action = action.Value };
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

        public static bool IsPathStillValid(List<PenguinNode>? path, Animal myAnimal, List<Cell> allCells, GameState state)
        {
            // Check that the path exists and that our animal is at the start of the path.
            if (path == null || path.Count == 0) return false;
            var firstNode = path[0];
            if (myAnimal.X != firstNode.X || myAnimal.Y != firstNode.Y)
                return false;

            // Build a grid for cell lookups.
            var grid = allCells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            foreach (var node in path)
            {
                if (!grid.TryGetValue((node.X, node.Y), out var content))
                    return false;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                    return false;
            }

            // Compute the danger along the path.
            int totalDanger = 0;
            foreach (var node in path)
            {
                // Compute the zookeeper penalty at this node.
                // (Assuming ComputeZookeeperPenalty is defined elsewhere and uses state.Zookeepers)
                totalDanger += ComputeZookeeperPenalty(node.X, node.Y, myAnimal, state);
            }

            // Define a threshold for path danger (tune this value as needed).

            if (totalDanger > DANGER_THRESHOLD)
            {
                // Path is too dangerous.
                return false;
            }

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

        public static List<PenguinNode>? ValidateOrComputePath(Animal myAnimal, Cell target, Dictionary<(int, int), CellContent> grid, GameState state)
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

        public static void LogCurrentPath(List<PenguinNode> path)
        {
            var coords = path.Select(n => $"({n.X},{n.Y})");
            Console.WriteLine("Current path: " + string.Join(" -> ", coords));
        }

        public static BotAction? ComputeNextMoveRateLimited(Animal myAnimal, List<PenguinNode> path)
        {
            if (path.Count == 0) return null;
            var firstStep = path[0];
            if (myAnimal.X != firstStep.X || myAnimal.Y != firstStep.Y)
                return null;
            if (path.Count < 2)
                return null;
            var secondStep = path[1];
            var action = GetDirection(firstStep.X, firstStep.Y, secondStep.X, secondStep.Y);
            if (!IGNORE_REPLAN && action.HasValue && LastMove.HasValue && action.Value == Opposite(LastMove.Value))
            {
                Console.WriteLine("Detected immediate reversal, checking for alternative step...");
                if (path.Count > 2)
                {
                    var thirdStep = path[2];
                    var altAction = GetDirection(secondStep.X, secondStep.Y, thirdStep.X, thirdStep.Y);
                    if (altAction.HasValue && altAction.Value != Opposite(LastMove.Value))
                    {
                        Console.WriteLine("Using alternative to avoid reversal.");
                        path.RemoveAt(1);
                        return altAction;
                    }
                }
                if (rng.NextDouble() < REVERSAL_RANDOM_SKIP_CHANCE)
                {
                    Console.WriteLine("Injecting random skip to break tie.");
                    return null;
                }
            }
            return action;
        }

        // --- Zookeeper-Aware A* Pathfinding Methods ---

        public static List<PenguinNode>? FindPath(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid,
            Animal myAnimal,
            GameState state)
        {
            NarrowPelletAvoidanceSet = IdentifyNarrowAndLeadInPellets(grid);

            var openSet = new List<PenguinNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new PenguinNode(
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

        public static List<PenguinNode> ReconstructPath(PenguinNode node)
        {
            var path = new List<PenguinNode>();
            while (node != null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        public static IEnumerable<PenguinNode> GetNeighborsPortal(
            PenguinNode current,
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
                if (NarrowPelletAvoidanceSet.Contains((nx, ny)))
                    additionalPenalty += CORRIDOR_PENALTY;

                int pelletBonus = (content == CellContent.Pellet) ? PELLET_BONUS : 0;
                int zookeeperPenalty = ComputeZookeeperPenalty(nx, ny, myAnimal, state);

                int newG = current.G + 1 + additionalPenalty + zookeeperPenalty - pelletBonus;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new PenguinNode(
                    nx, ny,
                    newG, newH,
                    current,
                    ComputeTieBreaker(nx, ny, state)
                );
            }
        }

        public static int ComputeTieBreaker(int x, int y, GameState state)
        {
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
            foreach (var zk in state.Zookeepers)
            {
                int dist = Manhattan(nx, ny, zk.X, zk.Y);
                // Avoid division by zero and scale penalty relative to risk (current score * 0.1)
                penalty += (int)(ZOOKEEPER_AVOIDANCE_FACTOR * (myAnimal.Score * 0.1) / Math.Max(dist, 1));
            }
            return penalty;
        }

        public static int Manhattan(int x, int y, int tx, int ty)
        {
            return Math.Abs(x - tx) + Math.Abs(y - ty);
        }

        public static BotAction? GetDirection(int currentX, int currentY, int nextX, int nextY)
        {
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


    }

    public class PenguinNode
    {
        public int X;
        public int Y;
        public int G;
        public int H;
        public int F => G + H;
        public PenguinNode? Parent;
        public int TieBreak { get; }

        public PenguinNode(int x, int y, int g, int h, PenguinNode? parent, int tieBreak = 0)
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
