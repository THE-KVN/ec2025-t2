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
    public static class Parrot
    {
        public static int lastTickCommandIssued = 0;
        public static Cell? PersistentTarget { get; set; } = null;
        public static List<ParrotNode>? PersistentPath { get; set; } = null;

        public static Dictionary<(int, int), int> VisitedCounts { get; set; } = new Dictionary<(int, int), int>();

        private static readonly Queue<(int, int)> RecentPositions = new Queue<(int, int)>();
        private const int RECENT_POS_QUEUE_SIZE = 10;

        private static int stuckCounter = 0;
        private const int STUCK_THRESHOLD = 2;

        private static readonly Random rng = new Random();

        public static BotAction? LastMove { get; set; } = null;
        public static Cell? CurrentTargetPellet = null;

        // **** New Properties for Execution Time Tracking ****
        public static int ExecutionTimeExceedCount { get; private set; } = 0;
        private static long TotalExecutionTime { get; set; } = 0;
        private static int TickCount { get; set; } = 0;
        public static double AverageExecutionTime => TickCount > 0 ? (double)TotalExecutionTime / TickCount : 0;

        // New fields to track lowest and highest execution times in milliseconds
        private static long minExecutionTime = long.MaxValue;
        private static long maxExecutionTime = 0;
        public static long LowestExecutionTime => TickCount > 0 ? minExecutionTime : 0;
        public static long HighestExecutionTime => maxExecutionTime;
        // ****************************************************

        public static BotCommand? ProcessState(GameState gameStateDTO, Guid id)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (gameStateDTO.Tick == lastTickCommandIssued) return null;

                var myAnimal = gameStateDTO.Animals.FirstOrDefault(x => x.Id == id);
                if (myAnimal == null)
                {
                    lastTickCommandIssued = gameStateDTO.Tick;
                    return null;
                }

                // Update visited counts
                var currentPos = (myAnimal.X, myAnimal.Y);
                if (!VisitedCounts.ContainsKey(currentPos))
                    VisitedCounts[currentPos] = 0;
                VisitedCounts[currentPos]++;

                // Detect loop/stuck
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

                var grid = gameStateDTO.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);

                // Validate or choose new target
                var target = ValidateOrFindTarget(myAnimal, gameStateDTO);
                CurrentTargetPellet = target;
                if (target == null)
                {
                    lastTickCommandIssued = gameStateDTO.Tick;
                    return null;
                }

                // Compute or reuse path
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

                // Next move
                BotAction? action = ComputeNextMove(myAnimal, path);
                if (!action.HasValue)
                {
                    Console.WriteLine("No valid move found. Clearing path.");
                    PersistentPath = null;
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
                stopwatch.Stop();
                long elapsed = stopwatch.ElapsedMilliseconds;
                TotalExecutionTime += elapsed;
                TickCount++;

                if (elapsed > 200)
                {
                    ExecutionTimeExceedCount++;
                }

                // Update lowest and highest execution times
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

            // 1) Attempt BFS-based cluster approach with weighting
            var clusterTarget = FindPelletInLargestConnectedClusterWeighted(myAnimal, state.Cells, alpha: 0.5);
            if (clusterTarget != null)
            {
                Console.WriteLine($"[ClusterMapping] Target from BFS approach: ({clusterTarget.X}, {clusterTarget.Y})");
                PersistentTarget = clusterTarget;
                PersistentPath = null;
                return clusterTarget;
            }

            // 2) Fallback: tie-break nearest pellet
            var tieBreakTarget = FindNearestPelletTieBreak(myAnimal.X, myAnimal.Y, state.Cells, LastMove);
            if (tieBreakTarget != null)
            {
                Console.WriteLine($"New target acquired (tie-break fallback): ({tieBreakTarget.X}, {tieBreakTarget.Y})");
                PersistentTarget = tieBreakTarget;
                PersistentPath = null;
            }
            return tieBreakTarget;
        }

        private static List<ParrotNode>? ValidateOrComputePath(Animal myAnimal, Cell target, Dictionary<(int, int), CellContent> grid)
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

        private static void LogCurrentPath(List<ParrotNode> path)
        {
            var coords = path.Select(n => $"({n.X},{n.Y})");
            Console.WriteLine("Current path: " + string.Join(" -> ", coords));
        }

        private static BotAction? ComputeNextMove(Animal myAnimal, List<ParrotNode> path)
        {
            if (path.Count == 0) return null;
            var firstStep = path[0];
            var action = GetDirection(myAnimal.X, myAnimal.Y, firstStep.X, firstStep.Y);

            if (action.HasValue && LastMove.HasValue && action.Value == Opposite(LastMove.Value))
            {
                Console.WriteLine("Detected immediate reversal, checking for alternative step...");
                if (path.Count > 1)
                {
                    var secondStep = path[1];
                    var altAction = GetDirection(myAnimal.X, myAnimal.Y, secondStep.X, secondStep.Y);
                    if (altAction.HasValue && altAction.Value != Opposite(LastMove.Value))
                    {
                        Console.WriteLine("Using alternative to avoid reversal.");
                        path.RemoveAt(0);
                        return altAction;
                    }
                }

                if (rng.NextDouble() < 0.3)
                {
                    Console.WriteLine("Injecting random skip to break tie.");
                    return null;
                }
            }
            return action;
        }

        // -----------------------------------------------------------------------
        // BFS-BASED CLUSTER FINDING + WEIGHTED SCORING
        // -----------------------------------------------------------------------
        private static Cell? FindPelletInLargestConnectedClusterWeighted(Animal myAnimal, List<Cell> allCells, double alpha)
        {
            // 1) Gather all pellet cells
            var pelletCells = allCells.Where(c => c.Content == CellContent.Pellet).ToList();
            if (pelletCells.Count == 0)
                return null;

            // Build a grid dictionary for BFS
            var grid = allCells.ToDictionary(c => (c.X, c.Y), c => c.Content);

            // 2) BFS for each unvisited pellet cell to find connected clusters
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

            // 3) For each cluster, compute:
            //    - clusterSize
            //    - clusterCenter (avgX, avgY)
            //    - distance = Manhattan(myAnimal.X, myAnimal.Y, avgX, avgY)
            //    - score = clusterSize - alpha * distance
            // Pick the cluster with the best score.
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

            // 4) Among the best cluster's pellets, pick the pellet nearest to our current position.
            var bestPellet = bestCluster
                .OrderBy(p => Manhattan(myAnimal.X, myAnimal.Y, p.X, p.Y))
                .First();

            return bestPellet;
        }

        /// <summary>
        /// BFS to collect all connected pellet cells starting from 'start'.
        /// "Connected" means you can move up/down/left/right without hitting a wall or spawn,
        /// and the cell is also a pellet.
        /// </summary>
        private static List<Cell> BFSCluster((int x, int y) start, Dictionary<(int, int), CellContent> grid, HashSet<(int, int)> visited)
        {
            var cluster = new List<Cell>();
            var queue = new Queue<(int x, int y)>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                // current is guaranteed to be a pellet from the caller
                cluster.Add(new Cell { X = current.x, Y = current.y, Content = CellContent.Pellet });

                // Explore neighbors
                int[,] dirs = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
                for (int i = 0; i < dirs.GetLength(0); i++)
                {
                    int nx = current.x + dirs[i, 0];
                    int ny = current.y + dirs[i, 1];
                    var npos = (nx, ny);

                    // Check if valid, unvisited, and also a pellet
                    if (!visited.Contains(npos) && grid.TryGetValue(npos, out var content))
                    {
                        if (content == CellContent.Pellet)
                        {
                            // Also ensure not blocked by wall/spawn
                            visited.Add(npos);
                            queue.Enqueue(npos);
                        }
                    }
                }
            }
            return cluster;
        }
        // -----------------------------------------------------------------------

        private static Cell? FindNearestPelletTieBreak(int startX, int startY, List<Cell> cells, BotAction? lastMove)
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

        public static List<ParrotNode>? FindPath(int startX, int startY, int targetX, int targetY,
                                                 Dictionary<(int, int), CellContent> grid)
        {
            var openSet = new List<ParrotNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new ParrotNode(startX, startY, 0, Manhattan(startX, startY, targetX, targetY));
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) => {
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

                foreach (var neighbor in GetNeighbors(current, grid, targetX, targetY))
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

        private static List<ParrotNode> ReconstructPath(ParrotNode node)
        {
            var path = new List<ParrotNode>();
            while (node.Parent != null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        private static IEnumerable<ParrotNode> GetNeighbors(
            ParrotNode current,
            Dictionary<(int, int), CellContent> grid,
            int targetX,
            int targetY)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int newX = current.X + directions[i, 0];
                int newY = current.Y + directions[i, 1];

                if (!grid.TryGetValue((newX, newY), out CellContent content))
                    continue;

                if (content == CellContent.Wall || content == CellContent.AnimalSpawn)
                    continue;

                int additionalPenalty = 0;
                if (current.Parent != null && newX == current.Parent.X && newY == current.Parent.Y)
                {
                    additionalPenalty += 2;
                }

                if (VisitedCounts.TryGetValue((newX, newY), out int visitCount))
                {
                    additionalPenalty += visitCount * 2;
                }

                int pelletBonus = 0;
                if (content == CellContent.Pellet)
                {
                    pelletBonus = 2;
                }

                int newG = current.G + 1 + additionalPenalty - pelletBonus;
                int newH = Manhattan(newX, newY, targetX, targetY);

                yield return new ParrotNode(newX, newY, newG, newH, current);
            }
        }

        private static int Manhattan(int x, int y, int tx, int ty)
        {
            return Math.Abs(x - tx) + Math.Abs(y - ty);
        }
    }

    public class ParrotNode
    {
        private static readonly Random rng = new Random();

        public int X;
        public int Y;
        public int G;
        public int H;
        public int F => G + H;
        public ParrotNode? Parent;

        public int TieBreak { get; }

        public ParrotNode(int x, int y, int g, int h, ParrotNode? parent = null)
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
