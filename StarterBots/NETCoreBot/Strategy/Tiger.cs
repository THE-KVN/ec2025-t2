using NETCoreBot.Enums;
using NETCoreBot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreBot.Strategy
{
    public static class Tiger
    {
        // --- Portal/Map Configuration ---
        private static int MapWidth = 30;  // Replaced at runtime
        private static int MapHeight = 20; // Replaced at runtime

        private static bool PortalLeft = true;
        private static bool PortalRight = true;
        private static bool PortalUp = true;
        private static bool PortalDown = true;

        public static int lastTickCommandIssued = 0;
        public static Cell? PersistentTarget { get; set; } = null;
        public static List<TigerNode>? PersistentPath { get; set; } = null;

        public static Dictionary<(int, int), int> VisitedCounts { get; set; }
            = new Dictionary<(int, int), int>();

        private static readonly Queue<(int, int)> RecentPositions = new Queue<(int, int)>();

        private static int stuckCounter = 0;
        private static readonly Random rng = new Random();

        public static BotAction? LastMove { get; set; } = null;
        public static Cell? CurrentTargetPellet = null;

        // Execution time tracking
        public static int ExecutionTimeExceedCount { get; private set; } = 0;
        private static long TotalExecutionTime { get; set; } = 0;
        private static int TickCount { get; set; } = 0;
        public static double AverageExecutionTime => TickCount > 0 ? (double)TotalExecutionTime / TickCount : 0;

        private static long minExecutionTime = long.MaxValue;
        private static long maxExecutionTime = 0;
        public static long LowestExecutionTime => TickCount > 0 ? minExecutionTime : 0;
        public static long HighestExecutionTime => maxExecutionTime;

        // Tweakable parameters
        public static double ALPHA = 0.7;
        public static int VISITED_TILE_PENALTY_FACTOR = 2;
        public static int PELLET_BONUS = 1;
        public static double REVERSAL_RANDOM_SKIP_CHANCE = 0.3;
        public static int REVERSING_TO_PARENT_PENALTY = 4;

        // Stuck detection
        private const int STUCK_THRESHOLD = 1;
        private const int RECENT_POS_QUEUE_SIZE = 4;

        // Partial re-plan: how often we re-check path validity
        private const int REPLAN_INTERVAL = 10;

        public static BotCommand? ProcessState(GameState gameStateDTO, Guid id)
        {
            var stopwatch = Stopwatch.StartNew();
            try
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


                // Update visited counts
                var currentPos = (myAnimal.X, myAnimal.Y);
                if (!VisitedCounts.ContainsKey(currentPos))
                    VisitedCounts[currentPos] = 0;
                VisitedCounts[currentPos]++;

                // Basic stuck detection
                if (RecentPositions.Contains(currentPos))
                {
                    stuckCounter++;
                    if (stuckCounter >= STUCK_THRESHOLD)
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
                if (gameStateDTO.Tick % REPLAN_INTERVAL == 0)
                {
                    if (!IsPathStillValid(PersistentPath, myAnimal, gameStateDTO.Cells))
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

                // Compute or reuse path
                var grid = gameStateDTO.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
                var path = ValidateOrComputePath(myAnimal, target, grid);
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

                Console.WriteLine($"Moving {action.Value}");
                LastMove = action.Value;
                PersistentPath?.RemoveAt(0);

                // If we reached the target
                if (myAnimal.X == target.X && myAnimal.Y == target.Y)
                {
                    Console.WriteLine("Target reached. Clearing persistent data.");
                    PersistentTarget = null;
                    PersistentPath = null;
                }

                lastTickCommandIssued = gameStateDTO.Tick;
                return new BotCommand { Action = action.Value };
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

                if (elapsed < minExecutionTime)
                {
                    minExecutionTime = elapsed;
                }
                if (elapsed > maxExecutionTime)
                {
                    maxExecutionTime = elapsed;
                }
            }
        }

        // --------------------------------------------
        // PARTIAL RE-PLAN: Check if path is still valid
        // --------------------------------------------
        private static bool IsPathStillValid(
            List<TigerNode>? path,
            Animal myAnimal,
            List<Cell> allCells)
        {
            if (path == null || path.Count == 0) return false;

            // If the path's first node doesn't match our current position, we call it invalid
            var firstNode = path[0];
            if (myAnimal.X != firstNode.X || myAnimal.Y != firstNode.Y)
            {
                return false;
            }

            // Also ensure no tile in the path has turned into a wall/spawn
            var grid = allCells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            foreach (var node in path)
            {
                if (!grid.TryGetValue((node.X, node.Y), out var content))
                    return false;
                if (content == CellContent.Wall
                    || content == CellContent.AnimalSpawn
                    || content == CellContent.ZookeeperSpawn)
                    return false;
            }
            return true;
        }

        // --------------------------------------------
        // TARGET SELECTION
        // --------------------------------------------
        private static Cell? ValidateOrFindTarget(Animal myAnimal, GameState state)
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

            // BFS-based cluster approach
            var clusterTarget = FindPelletInLargestConnectedClusterWeighted(myAnimal, state.Cells, ALPHA);
            if (clusterTarget != null)
            {
                Console.WriteLine($"[ClusterMapping] Target from BFS approach: ({clusterTarget.X}, {clusterTarget.Y})");
                PersistentTarget = clusterTarget;
                PersistentPath = null;
                return clusterTarget;
            }

            // Fallback: tie-break nearest pellet
            var tieBreakTarget = FindNearestPelletTieBreak(myAnimal.X, myAnimal.Y, state.Cells, LastMove);
            if (tieBreakTarget != null)
            {
                Console.WriteLine($"New target acquired (tie-break fallback): ({tieBreakTarget.X}, {tieBreakTarget.Y})");
                PersistentTarget = tieBreakTarget;
                PersistentPath = null;
            }
            return tieBreakTarget;
        }

        private static List<TigerNode>? ValidateOrComputePath(
            Animal myAnimal,
            Cell target,
            Dictionary<(int, int), CellContent> grid)
        {
            if (PersistentPath != null && PersistentPath.Count > 0)
            {
                Console.WriteLine("Using persistent path.");
                LogCurrentPath(PersistentPath);
                return PersistentPath;
            }

            var path = FindPath(myAnimal.X, myAnimal.Y, target.X, target.Y, grid);
            PersistentPath = path;
            if (path != null && path.Count > 0)
            {
                Console.WriteLine("Computed new path.");
                LogCurrentPath(path);
            }
            return path;
        }

        private static void LogCurrentPath(List<TigerNode> path)
        {
            var coords = path.Select(n => $"({n.X},{n.Y})");
            Console.WriteLine("Current path: " + string.Join(" -> ", coords));
        }

        // --------------------------------------------
        // RATE-LIMITED NEXT MOVE
        // --------------------------------------------
        private static BotAction? ComputeNextMoveRateLimited(Animal myAnimal, List<TigerNode> path)
        {
            if (path.Count == 0) return null;

            // If we're not physically on path[0], do nothing
            var firstStep = path[0];
            if (myAnimal.X != firstStep.X || myAnimal.Y != firstStep.Y)
            {
                // Not at the tile that demands next step -> skip
                return null;
            }

            // If there's no second step, we can't move further
            if (path.Count < 2)
            {
                return null;
            }

            var secondStep = path[1];
            var action = GetDirection(firstStep.X, firstStep.Y, secondStep.X, secondStep.Y);

            // Handle immediate reversal checks
            if (action.HasValue && LastMove.HasValue && action.Value == Opposite(LastMove.Value))
            {
                Console.WriteLine("Detected immediate reversal, checking for alternative step...");
                if (path.Count > 2)
                {
                    var thirdStep = path[2];
                    var altAction = GetDirection(secondStep.X, secondStep.Y, thirdStep.X, thirdStep.Y);
                    if (altAction.HasValue && altAction.Value != Opposite(LastMove.Value))
                    {
                        Console.WriteLine("Using alternative to avoid reversal.");
                        path.RemoveAt(1); // skip the second step
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

        // BFS-based cluster approach
        private static Cell? FindPelletInLargestConnectedClusterWeighted(
            Animal myAnimal,
            List<Cell> allCells,
            double alpha)
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

            var bestPellet = bestCluster
                .OrderBy(p => Manhattan(myAnimal.X, myAnimal.Y, p.X, p.Y))
                .First();

            return bestPellet;
        }

        private static List<Cell> BFSCluster(
            (int x, int y) start,
            Dictionary<(int, int), CellContent> grid,
            HashSet<(int, int)> visited)
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

        private static Cell? FindNearestPelletTieBreak(
            int startX,
            int startY,
            List<Cell> cells,
            BotAction? lastMove)
        {
            int bestDistance = int.MaxValue;
            var pellets = new List<Cell>();
            foreach (var c in cells)
            {
                if (c.Content == CellContent.Pellet)
                {
                    int dist = Math.Abs(startX - c.X) + Math.Abs(startY - c.Y);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                    }
                    pellets.Add(c);
                }
            }
            if (pellets.Count == 0) return null;

            var closest = pellets
                .Where(p => Math.Abs(startX - p.X) + Math.Abs(startY - p.Y) == bestDistance)
                .ToList();
            if (closest.Count == 0) return null;

            var aligned = closest.Where(p => IsAligned(lastMove, startX, startY, p)).ToList();
            if (aligned.Count > 0)
                return aligned[0];
            else
                return closest[0];
        }

        private static bool IsAligned(BotAction? lastMove, int myX, int myY, Cell pellet)
        {
            if (!lastMove.HasValue) return false;
            switch (lastMove.Value)
            {
                case BotAction.Up: return pellet.Y < myY;
                case BotAction.Down: return pellet.Y > myY;
                case BotAction.Left: return pellet.X < myX;
                case BotAction.Right: return pellet.X > myX;
                default: return false;
            }
        }

        private static BotAction? GetDirection(int currentX, int currentY, int nextX, int nextY)
        {
            if (nextX > currentX) return BotAction.Right;
            if (nextX < currentX) return BotAction.Left;
            if (nextY > currentY) return BotAction.Down;
            if (nextY < currentY) return BotAction.Up;
            return null;
        }

        private static BotAction Opposite(BotAction action)
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

        // Pathfinding with partial-edge portals
        public static List<TigerNode>? FindPath(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid)
        {
            var openSet = new List<TigerNode>();
            var closedSet = new HashSet<(int, int)>();

            // Start node is your actual current position
            var startNode = new TigerNode(startX, startY, 0, Manhattan(startX, startY, targetX, targetY));
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                // Sort with tie-break
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
                {
                    return ReconstructPath(current);
                }

                closedSet.Add((current.X, current.Y));

                foreach (var neighbor in GetNeighborsPortal(current, grid, targetX, targetY))
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

        private static List<TigerNode> ReconstructPath(TigerNode node)
        {
            var path = new List<TigerNode>();
            // Include the start node (the one with Parent == null)
            while (node != null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        private static IEnumerable<TigerNode> GetNeighborsPortal(
            TigerNode current,
            Dictionary<(int, int), CellContent> grid,
            int targetX,
            int targetY)
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

                if (content == CellContent.Wall
                    || content == CellContent.AnimalSpawn
                    || content == CellContent.ZookeeperSpawn)
                    continue;

                int additionalPenalty = 0;
                if (current.Parent != null && nx == current.Parent.X && ny == current.Parent.Y)
                {
                    additionalPenalty += REVERSING_TO_PARENT_PENALTY;
                }

                if (VisitedCounts.TryGetValue((nx, ny), out int visitCount))
                {
                    additionalPenalty += visitCount * VISITED_TILE_PENALTY_FACTOR;
                }

                int pelletBonus = (content == CellContent.Pellet) ? PELLET_BONUS : 0;

                int newG = current.G + 1 + additionalPenalty - pelletBonus;
                int newH = Manhattan(nx, ny, targetX, targetY);

                yield return new TigerNode(nx, ny, newG, newH, current);
            }
        }

        private static int Manhattan(int x, int y, int tx, int ty)
        {
            return Math.Abs(x - tx) + Math.Abs(y - ty);
        }
    }

    public class TigerNode
    {
        private static readonly Random rng = new Random();

        public int X;
        public int Y;
        public int G;
        public int H;
        public int F => G + H;
        public TigerNode? Parent;

        public int TieBreak { get; }

        public TigerNode(int x, int y, int g, int h, TigerNode? parent = null)
        {
            X = x;
            Y = y;
            G = g;
            H = h;
            Parent = parent;
            TieBreak = rng.Next(int.MinValue, int.MaxValue);
        }
    }
}
